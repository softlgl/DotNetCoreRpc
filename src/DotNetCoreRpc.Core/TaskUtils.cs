using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Core
{
    public class TaskUtils
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object>> _asTaskFuncCache = new ConcurrentDictionary<Type, Func<object, object>>();
        private static readonly ConcurrentDictionary<Type, Func<object, object>> _asValueTaskFuncCache = new ConcurrentDictionary<Type, Func<object, object>>();
        private static readonly ConcurrentDictionary<Type, Func<object, object>> _resultFuncCache = new ConcurrentDictionary<Type, Func<object, object>>();
        private static readonly ConcurrentDictionary<TypeInfo, Func<object, Task>> _valueTaskAsTaskFuncCache = new ConcurrentDictionary<TypeInfo, Func<object, Task>>();
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object?[]?, object?>> _methodFuncCache = new ConcurrentDictionary<MethodInfo, Func<object, object?[]?, object?>>();
        private static readonly ConcurrentDictionary<Type, Func<object, Task<object>>> _unwrapTaskCache = new ConcurrentDictionary<Type, Func<object, Task<object>>>();
        private static readonly ConcurrentDictionary<Type, Func<object, Task<object>>> _unwrapValueTaskCache = new ConcurrentDictionary<Type, Func<object, Task<object>>>();

        public static Func<object, object> TaskResultFunc(Type returnType)
        {
            var func = _asTaskFuncCache.GetOrAdd(returnType, type => {
                var resultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(returnType);
                ParameterExpression source = Expression.Parameter(typeof(object), "result");
                var instanceCast = Expression.Convert(source, returnType);
                var callExpr = Expression.Call(resultMethod, instanceCast);
                var expr = Expression.Lambda<Func<object, object>>(callExpr, source).Compile();
                return expr;
            });
            return func;
        }

        public static Func<object, object> ValueTaskResultFunc(Type returnType)
        {
            var func = _asValueTaskFuncCache.GetOrAdd(returnType, type =>
            {
                var valueType = typeof(ValueTask<>).MakeGenericType(returnType);
                ParameterExpression source = Expression.Parameter(typeof(object), "result");
                UnaryExpression instanceCast = Expression.Convert(source, returnType);
                var newExpr = Expression.New(valueType.GetConstructor(new[] { returnType }), instanceCast);
                var convertBody = Expression.Convert(newExpr, typeof(object));
                var expr = Expression.Lambda<Func<object, object>>(convertBody, source).Compile();
                return expr;
            });
            return func;
        }

        public static Func<object, object> CreateFuncToGetTaskResult(Type type)
        {
            var func = _resultFuncCache.GetOrAdd(type, typeInfo => {
                var parameter = Expression.Parameter(typeof(object), "type");
                var convertedParameter = Expression.Convert(parameter, typeInfo);
                var property = Expression.Property(convertedParameter, nameof(Task<int>.Result));
                var convertedProperty = Expression.Convert(property, typeof(object));
                var exp = Expression.Lambda<Func<object, object>>(convertedProperty, parameter).Compile();
                return exp;
            });
            return func;
        }

        public static Task ValueTaskWithResultToTask(object value, TypeInfo valueTypeInfo)
        {
            var func = _valueTaskAsTaskFuncCache.GetOrAdd(valueTypeInfo, k =>
            {
                var parameter = Expression.Parameter(typeof(object), "type");
                var convertedParameter = Expression.Convert(parameter, k);
                var method = k.GetMethod(nameof(ValueTask<int>.AsTask));
                var property = Expression.Call(convertedParameter, method);
                var convertedProperty = Expression.Convert(property, typeof(Task));
                var exp = Expression.Lambda<Func<object, Task>>(convertedProperty, parameter);
                return exp.Compile();
            });
            return func(value);
        }

        public static Func<object, object?[]?, object?> InvokeMethod(MethodInfo methodInfo)
        {
            var methodFunc = _methodFuncCache.GetOrAdd(methodInfo, method => 
            {
                var targetParameter = Expression.Parameter(typeof(object), "target");
                var parametersParameter = Expression.Parameter(typeof(object?[]), "parameters");

                var paramInfos = method.GetParameters();
                var parameters = new List<Expression>(paramInfos.Length);
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                    var valueCast = Expression.Convert(valueObj, paramInfos[i].ParameterType);

                    parameters.Add(valueCast);
                }

                Type targetType = method.DeclaringType;
                var instanceCast = Expression.Convert(targetParameter, targetType);
                var methodCall = Expression.Call(instanceCast, method, parameters);

                if (methodCall.Type == typeof(void))
                {
                    var lambdaAction = Expression.Lambda<Action<object, object?[]?>>(methodCall, targetParameter, parametersParameter);
                    // 编译一次再复用，避免每次调用都重新编译表达式
                    var compiledAction = lambdaAction.Compile();
                    return (target, parameters) =>
                    {
                        compiledAction.Invoke(target, parameters);
                        return null;
                    };
                }

                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambdaFunc = Expression.Lambda<Func<object, object?[]?, object?>>(castMethodCall, targetParameter, parametersParameter);
                return lambdaFunc.Compile();
            });

            return methodFunc;
        }

        public static bool IsAsyncMethod(MethodInfo method)
        {
            bool isDefAsync = Attribute.IsDefined(method, typeof(AsyncStateMachineAttribute), false);
            bool isTaskType = CheckMethodReturnTypeIsTaskType(method);
            bool isAsync = isDefAsync || isTaskType;
            return isAsync;
        }

        public static bool CheckMethodReturnTypeIsTaskType(MethodInfo method)
        {
            var methodReturnType = method.ReturnType.GetTypeInfo();
            return methodReturnType.IsAsync();
        }

        /// <summary>
        /// 异步解包方法返回值：对 Task&lt;T&gt;/ValueTask&lt;T&gt; 仅 await 一次并返回结果对象，
        /// 避免同步 .Result 阻塞线程与 ValueTask 二次消费。无返回值的异步方法 await 后返回 null。
        /// </summary>
        public static async Task<object> UnwrapAsync(object returnValue, TypeInfo returnValueType)
        {
            if (returnValue == null)
            {
                return null;
            }

            if (returnValueType.IsTaskWithResult())
            {
                var resultType = returnValueType.GetGenericArguments()[0];
                var func = _unwrapTaskCache.GetOrAdd(resultType, type =>
                {
                    // 表达式直接调用泛型解包方法并编译，缓存后热路径无反射 Invoke 与参数数组分配
                    var method = typeof(TaskUtils).GetMethod(nameof(AwaitTaskResultImpl), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
                    ParameterExpression source = Expression.Parameter(typeof(object), "obj");
                    var callExpr = Expression.Call(method, Expression.Convert(source, typeof(Task<>).MakeGenericType(type)));
                    var convertBody = Expression.Convert(callExpr, typeof(Task<object>));
                    return Expression.Lambda<Func<object, Task<object>>>(convertBody, source).Compile();
                });
                return await func(returnValue).ConfigureAwait(false);
            }

            if (returnValueType.IsValueTaskWithResult())
            {
                var resultType = returnValueType.GetGenericArguments()[0];
                var func = _unwrapValueTaskCache.GetOrAdd(resultType, type =>
                {
                    var method = typeof(TaskUtils).GetMethod(nameof(AwaitValueTaskResultImpl), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
                    ParameterExpression source = Expression.Parameter(typeof(object), "obj");
                    var callExpr = Expression.Call(method, Expression.Convert(source, typeof(ValueTask<>).MakeGenericType(type)));
                    var convertBody = Expression.Convert(callExpr, typeof(Task<object>));
                    return Expression.Lambda<Func<object, Task<object>>>(convertBody, source).Compile();
                });
                return await func(returnValue).ConfigureAwait(false);
            }

            if (returnValueType.IsTask() || returnValueType.IsTaskWithVoidTaskResult())
            {
                await ((Task)returnValue).ConfigureAwait(false);
                return null;
            }

            if (returnValueType.IsValueTask())
            {
                await ((ValueTask)returnValue).ConfigureAwait(false);
                return null;
            }

            return returnValue;
        }

        private static async Task<object> AwaitTaskResultImpl<T>(Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }

        private static async Task<object> AwaitValueTaskResultImpl<T>(ValueTask<T> valueTask)
        {
            return await valueTask.ConfigureAwait(false);
        }
    }
}
