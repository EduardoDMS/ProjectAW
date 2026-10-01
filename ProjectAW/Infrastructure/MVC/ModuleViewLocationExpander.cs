using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;

namespace ProjectAW.Infrastructure.MVC
{
    public class ModuleViewLocationExpander:IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var viewModule = context.ActionContext.RouteData.Values["viewModule"]?.ToString();

            if (!string.IsNullOrEmpty(viewModule))
            {
                context.Values["viewModule"] = viewModule;
            }
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if(context.Values.TryGetValue("viewModule",out var viewModule))
            {
                yield return $"/Modules/{viewModule}/Views/{{0}}.cshtml";
            }

            foreach(var location in viewLocations)
            {
                yield return location;
            }
        }
    }
}
