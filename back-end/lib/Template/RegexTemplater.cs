using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sara.Lib.Template
{
    /// <summary>
    /// A very simple Template engine using regular expressions.
    /// Supports named variables and for loops.
    /// </summary>
    public static class RegexTemplater
    {
        /// <summary>
        /// Renders a template using a set of variables. To refer to the variables use '$' escape.
        /// Properties of objects can be used, for example 'Hello $Customer.Name'. The syntax for
        /// loops is $foreach($iterator in $expression){ [put text here. Variables must be
        /// prefixed with $] }
        /// </summary>
        /// <param name="template"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static string Render(string template, Dictionary<string, object> variables)
        {
            // for loops
            // syntax is @foreach(@iterator in @expression) { ... your text ... }
            // variables in the text area must have '@'. character before them
            var fl = new Regex(@"\$foreach\s*\(\s*\$(?<iterator>[^)]*)\s+in\s+\$(?<enumerable>[^)]*)\)\s*\{\s*(?<code>[^}]*)\s*\}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            template = fl.Replace(template, m =>
            {
                var iterator = m.Groups["iterator"].Value;
                var enumerable = m.Groups["enumerable"].Value;
                var code = m.Groups["code"].Value;

                // get enumerable object
                IEnumerable<Object> enumer = GetDictionaryElement(variables, enumerable) as IEnumerable<Object>;

                if (enumer != null)
                {
                    string loopedText = "";
                    foreach (var iter in enumer)
                    {
                        Dictionary<string, object> iterationVariables = new Dictionary<string, object>();
                        iterationVariables = GetProperties(iter, iterationVariables, iterator, true);
                        loopedText += Render(code, iterationVariables);
                    }
                    return loopedText;
                }
                else
                    return string.Empty;    // empty string
            });

            // simple expansion of variables
            var r = new Regex(@"\$(?<name>([A-Za-z\._]*))", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            template = SimpleReplace(template, r, variables);

            return template;
        }

        public static string Render(string template, object model)
        {
            var variables = GetProperties(model, new Dictionary<string, object>(), string.Empty, false);
            return Render(template, variables);
        }

        private static Dictionary<string, object> GetProperties(object model, Dictionary<string, object> properties, string prefix, bool includeRoot)
        {
            if (model != null)
            {
                Type modelType = model.GetType();

                if (includeRoot)
                    properties.Add(prefix, model);

                // Get properties declared on the type itself (no inherited members)
                foreach (var pi in model.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public))
                {
                    if (!IsIndexedProperty(pi))
                    {
                        object nextval = pi.GetValue(model, null);
                        string nextprefix = prefix + (string.IsNullOrEmpty(prefix) ? string.Empty : ".") + pi.Name;
                        properties = GetProperties(nextval, properties, nextprefix, true);
                    }
                }
            }
            return properties;
        }

        private static bool IsIndexedProperty(PropertyInfo pi)
        {
            return (pi.GetIndexParameters().Count() > 0);
        }

        // possibly make this extension method
        private static object GetDictionaryElement(Dictionary<string, object> dictionary, string key)
        {
            object ret = null;
            if (dictionary.TryGetValue(key, out ret))
                return ret;
            else
                return null;
        }

        private static string SimpleReplace(string template, Regex pattern, Dictionary<string, object> variables)
        {
            var result = pattern.Replace(template, m =>
            {
                var key = m.Groups["name"].Value;
                object val;
                if (variables.TryGetValue(key, out val))
                    return val.ToString();
                else
                    return "";  // If variable not present, put empty string in template.
            });
            return result;
        }
    }
}
