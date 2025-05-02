using Microsoft.IdentityModel.Tokens;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;
using wfDocServices.Constants;

namespace wfDocServices.BL
{
    public class RabbitqueueBL : IDisposable
    {
        private static readonly Lazy<RabbitqueueBL> _instance = new Lazy<RabbitqueueBL>(() => new RabbitqueueBL());
        private UtilityBL utilitylogic = new UtilityBL();
        private IConnection connection;
        private IChannel channel;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private bool isDisposed = false;

        private RabbitqueueBL() { }

        public static RabbitqueueBL Instance => _instance.Value;

        private ConnectionFactory GetConnectionFactory(string rabbitqhost, string rabbitquser, string rabbitqpass)
        {
           // string t = utilitylogic.DecryptPassword("Jz9WnOAL5ugTOG6GDBLzc6ea0IEBDRxqXm/6dv0GX9g=");
            return new ConnectionFactory()
            {
                HostName = rabbitqhost,
                UserName = rabbitquser,
                Password = rabbitqpass,
                AutomaticRecoveryEnabled = true, // this already helps basic auto-recovery
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<IChannel> RabbitqueueChannel()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(RabbitqueueBL));

            try
            {
                // reuse the channel
                if (channel != null && channel.IsOpen)
                {
                    return channel;
                }

                var factory = GetConnectionFactory(utilitylogic.ReadConfigFile(ConfigConstants.rabbitqueueHost), utilitylogic.ReadConfigFile(ConfigConstants.rabbitqueueUser), utilitylogic.ReadConfigFile(ConfigConstants.rabbitqueuePass));
               //var factory = GetConnectionFactory("192.168.9.120", "test", "epicor");

                if (connection != null && connection.IsOpen)
                {
                    channel = await connection.CreateChannelAsync();
                }
                else
                {
                    connection?.Dispose();
                    connection = await factory.CreateConnectionAsync();
                    channel = await connection.CreateChannelAsync();

                    // Listen for connection shutdowns
                    connection.ConnectionShutdownAsync += OnConnectionShutdown;
                }

                return channel;
            }
            catch (Exception ex)
            {
                logger.Info($"{ex}. ");
            }
            return null;
        }
        private async Task OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (isDisposed)
                return;

            Console.WriteLine($"RabbitMQ connection shutdown detected: {e.ReplyText}");

            // Try reconnect
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // wait before retry
                await RabbitqueueChannel(); // attempt to reconnect
                Console.WriteLine("RabbitMQ reconnected successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ reconnection failed: {ex.Message}");
                // Optionally, add retry mechanism or escalate
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || isDisposed)
                return;

            isDisposed = true;

            if (channel != null)
            {
                channel.CloseAsync();
                channel.Dispose();
                channel = null;
            }
            if (connection != null)
            {
                connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                connection.CloseAsync();
                connection.Dispose();
                connection = null;
            }
            if (utilitylogic != null)
            {
                utilitylogic.Dispose();
                utilitylogic = null;
            }
        }
    }
}
