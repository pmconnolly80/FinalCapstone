using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BeersList.Startup))]
namespace BeersList
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
