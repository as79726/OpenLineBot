using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenLineBot {
    public class Startup {
        public Startup (IConfiguration configuration, IHostEnvironment hostingEnvironment) {
            Configuration = configuration;
            HostEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostEnvironment HostEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices (IServiceCollection services) {
            services.AddControllers ();
            SecretInfo secretInfo = new SecretInfo ();
            Configuration.Bind ("LINE-Bot-Setting", secretInfo);
            services.AddSingleton<SecretInfo> (secretInfo);
            GoogleCredential credential = GoogleCredential.FromFile (HostEnvironment.ContentRootPath + "/auth.json");
            ChannelCredentials channelCredentials = credential.ToChannelCredentials ();
            Channel channel = new Channel (FirestoreClient.DefaultEndpoint.ToString (), channelCredentials);
            FirestoreClient firestoreClient = FirestoreClient.Create (channel);
            FirestoreDb db = FirestoreDb.Create (Configuration.GetSection ("FirseBase:ProjectId").Value, firestoreClient);
            services.AddSingleton<FirestoreDb> (db);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }

            app.UseHttpsRedirection ();

            app.UseRouting ();

            app.UseAuthorization ();

            app.UseEndpoints (endpoints => {
                endpoints.MapControllers ();
            });
        }
    }

    public class SecretInfo {
        public string AdminId { get; set; }
        public string ChannelAccessToken { get; set; }
    }
}