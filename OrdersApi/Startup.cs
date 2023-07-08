using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OrdersApi.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfig>(Configuration.GetSection("CustomConfig")?.Get<Config>());
            //services.AddDbContext<OrdersContext>(options =>
            //   options.UseSqlServer(Configuration.GetConnectionString("OrdersConnection")));
            AddDbContexts(services);
            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddControllers().AddDapr();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OrdersApi", Version = "v1" });
            });
        }

        public void AddDbContexts(IServiceCollection services)
        {

            services.AddDbContext<OrdersContext>(opt =>
            {
                var connectionString = Configuration.GetConnectionString("sql-order") ??
                "name=OrdersConnection";
                Console.Write("ConString:" + connectionString + " ");
                opt.UseSqlServer(connectionString, opt => opt.EnableRetryOnFailure(5));
            }, ServiceLifetime.Transient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrdersApi v1"));
            }

            app.UseRouting();
            app.UseCloudEvents();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapSubscribeHandler();
            });
            TryRunMigrations(app);
        }

        private void TryRunMigrations(IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<IConfig>();
            if (config?.RunDbMigrations == true)
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersContext>();
                    dbContext.Database.Migrate();
                }
            }


        }
    }
}
