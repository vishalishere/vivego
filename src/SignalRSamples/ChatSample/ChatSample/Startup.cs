using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using vivego.PublishSubscribe;
using vivego.PublishSubscribe.Grpc;
using vivego.Serializer.Wire;
using vivego.SignalR.PublishSubscribe;

namespace SignalRChatSample
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSignalR();

			IPublishSubscribe publishSubscribe = new PublishSubscribeBuilder(new WireSerializer())
				//.GrpcPublishSubscribe("localhost")
				.Build();
			services.AddSingleton(publishSubscribe);
			services.AddSignalRPublishSubscribeHubLifetimeManager();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseFileServer();
			app.UseSignalR(routes => { routes.MapHub<ChatHub>("chat"); });
		}
	}
}