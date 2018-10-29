// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRabbitMqListener.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the IRabbitMqListener type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Realtime.Interfaces
{
    using System.Threading;

    /// <summary>
    /// The RabbitMqListener interface.
    /// </summary>
    public interface IRabbitMqListener
    {
        /// <summary>
        /// The get message.
        /// </summary>
        /// <param name="hostname">
        /// The hostname.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="messageReceivedWaitHandle">
        /// The message received wait handle.
        /// </param>
        /// <param name="channelCreatedWaitHandle">
        /// The channel created wait handle.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        string GetMessage(string hostname, CancellationToken token, AutoResetEvent messageReceivedWaitHandle, AutoResetEvent channelCreatedWaitHandle);

        /// <summary>
        /// The start listening.
        /// </summary>
        /// <param name="mirthhostname">
        /// The mirthhostname.
        /// </param>
        /// <returns>
        /// The <see cref="CancellationTokenSource"/>.
        /// </returns>
        CancellationTokenSource StartListening(string mirthhostname);
    }
}