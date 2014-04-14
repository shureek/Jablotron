namespace CJablotron
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;

    public static class Extensions
    {
        public static string GetDescription(this Exception ex)
        {
            StringBuilder builder = new StringBuilder();
            for (Exception exception = ex; exception != null; exception = exception.InnerException)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.Append('\t');
                }
                builder.AppendFormat("{0}: {1}", exception.GetType().FullName, exception.Message);
            }
            return builder.ToString();
        }

        public static T GetException<T>(this Exception ex) where T: class
        {
            if (ex is T)
            {
                return (ex as T);
            }
            if (ex.InnerException != null)
            {
                return ex.InnerException.GetException<T>();
            }
            return default(T);
        }

        public static string ReplaceChars(this string str, string oldChars, string newChars)
        {
            if (oldChars == null)
                throw new ArgumentNullException("oldChars");
            if (newChars == null)
                throw new ArgumentNullException("newChars");
            if (oldChars.Length != newChars.Length)
                throw new ArgumentException("oldChars length must match newChars length");

            string result = str;
            for (int i = 0; i < oldChars.Length; i++)
                result = result.Replace(oldChars[i], newChars[i]);
            return result;
        }
    }
}

