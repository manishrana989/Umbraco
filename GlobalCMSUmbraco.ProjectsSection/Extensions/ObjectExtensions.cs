using System.Reflection;

namespace GlobalCMSUmbraco.ProjectsSection.Extensions
{
    public static class ObjectExtension
    {
        public static T Get<T>(this object obj, string name)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            var field = type.GetField(name, flags);
            if (field!= null)
                return (T)field.GetValue(obj);
             
            var property = type.GetProperty(name, flags);
            if (property != null)
                return (T)property.GetValue(obj,null);
 
            return default;
        }
 
        public static void Set(this object obj, string name, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            
            var field = type.GetField(name, flags);
            if (field != null)
                field.SetValue(obj,value);
 
            var property = type.GetProperty(name, flags);
            if (property != null)
                property.SetValue(obj,value, null);
        }
 
        public static T Call<T>(this object obj, string name, params object[] param)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = obj.GetType();
            var method = type.GetMethod(name, flags);
            return (T)method?.Invoke(obj, param);
        }
    }
}
