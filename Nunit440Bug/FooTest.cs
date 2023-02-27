using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Reflection;
using System.Runtime.Loader;

namespace Nunit440Bug
{
    public class FooTest
    {
        [Test]
        public void SystemWebServicesTest() => RunTest(
            "..\\..\\..\\..\\OtherAssembly\\bin\\Debug\\net6.0",
            "OtherAssembly.dll"
        );


        private void RunTest(
            string assemblyFolder,
            string assemblyName)
        {
            //If we are running a release build of the tests, look for release builds of the rest of the code
            if (Environment.CurrentDirectory.Contains("\\Release\\", StringComparison.CurrentCulture))
            {
                assemblyFolder = assemblyFolder.Replace("\\Debug\\", "\\Release\\");
            }

            string assemblyPath = Path.Combine(assemblyFolder, assemblyName);
            if (!File.Exists(assemblyPath))
            {
                Assert.Fail($"{assemblyPath} does not exist. Have you built the full solution?.");
                return;
            }

            var context = new AssemblyLoadContext("test", true);
            context.EnterContextualReflection();
            context.Resolving += (context, childAssemblyName) =>
            {
                var tryPath = Path.Combine(assemblyFolder, childAssemblyName.Name + ".dll");
                if (File.Exists(tryPath))
                {
                    return context.LoadFromAssemblyPath(Path.Combine(Environment.CurrentDirectory, tryPath));
                }
                return null;
            };

            try
            {
                var assembly = context.LoadFromAssemblyPath(Path.Combine(Environment.CurrentDirectory, assemblyPath));
                var foo = assembly.GetTypes().FirstOrDefault(t => t.Name == "Foo");
                var bar = foo.GetMethod("Bar", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var services = new ServiceCollection();
                bar.Invoke(null, new[] { services }); //4.4.0 test adaptor fails here, 4.3.1 works
                context.Unload();
            }
            finally
            {
                AssemblyLoadContext.Default.EnterContextualReflection();
            }

        }



    }
}