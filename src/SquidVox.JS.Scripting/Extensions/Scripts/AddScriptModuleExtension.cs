using DryIoc;
using SquidVox.Core.Data.Scripts.Container;
using SquidVox.Core.Extensions.Container;

namespace SquidVox.JS.Scripting.Extensions.Scripts;

/// <summary>
///     Extension methods for registering script modules in the dependency injection container
/// </summary>
public static class AddScriptModuleExtension
{
    public static IContainer AddScriptModule(this IContainer container, Type scriptModule)
    {
        if (scriptModule == null)
        {
            throw new ArgumentNullException(nameof(scriptModule), "Script module type cannot be null.");
        }

        container.AddToRegisterTypedList(new ScriptModuleData(scriptModule));

        container.Register(scriptModule, Reuse.Singleton);


        return container;
    }

    public static IContainer AddScriptModule<TScriptModule>(this IContainer container) where TScriptModule : class
    {
        return container.AddScriptModule(typeof(TScriptModule));
    }
}
