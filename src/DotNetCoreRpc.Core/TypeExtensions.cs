using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Core
{
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<TypeInfo, bool> _isTaskOfTCache = new ConcurrentDictionary<TypeInfo, bool>();
        private static readonly ConcurrentDictionary<TypeInfo, bool> _isValueTaskOfTCache = new ConcurrentDictionary<TypeInfo, bool>();
        private static readonly Type _voidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult", false);

        public static bool IsTask(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }
            return typeInfo.AsType() == typeof(Task);
        }

        public static bool IsTaskWithResult(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }
            return _isTaskOfTCache.GetOrAdd(typeInfo, key => key.IsGenericType && typeof(Task).GetTypeInfo().IsAssignableFrom(key));
        }

        public static bool IsTaskWithVoidTaskResult(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return typeInfo.GenericTypeArguments?.Length > 0 && typeInfo.GenericTypeArguments[0] == _voidTaskResultType;
        }

        public static bool IsValueTask(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }
            return typeInfo.AsType() == typeof(ValueTask);
        }

        public static bool IsValueTaskWithResult(this TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }
            return _isValueTaskOfTCache.GetOrAdd(typeInfo, key => key.IsGenericType && key.GetGenericTypeDefinition() == typeof(ValueTask<>));
        }

        public static bool IsAsync(this TypeInfo typeInfo)
        {
            return typeInfo.IsTask() 
                || typeInfo.IsTaskWithResult()
                || typeInfo.IsTaskWithVoidTaskResult() 
                || typeInfo.IsValueTask()
                || typeInfo.IsValueTaskWithResult();
        }
    }
}
