using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace angrydev.Wol
{
    public class WakeOnLanClient : UdpClient
    {
        private void SetSocketOptions()
        {
            if (!Active) return;

            Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);

            // bind to all active network interfaces
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in networkInterfaces)
            {
                if (OperationalStatus.Up != nic.OperationalStatus)
                    continue; // this adapter is off or not connected
                if (!nic.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection

                var ipProps = nic.GetIPProperties();
                if (!ipProps.MulticastAddresses.Any())
                    continue; // most of VPN adapters will be skipped

                // bind to all available interfaces with available props
                var ip4Props = ipProps.GetIPv4Properties();
                var ip6Props = ipProps.GetIPv6Properties();

                if (ip4Props != null)
                    Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(ip4Props.Index));
                // if only ipv6 is configured, add adapter by index, 
                // otherwise the index is already added by ipv4 props
                if (ip4Props == null && ip6Props != null)
                    Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(ip6Props.Index));
            }
        }

        public static void WakeUp(string macAddress)
        {
            using (var udpClient = new WakeOnLanClient())
            {
                udpClient.SendWakeUp(macAddress);
            }
        }

        public void SendWakeUp(string macAddress)
        {
            var mac = ParseMac(macAddress);
            SendWakeUp(mac);
        }

        public void SendWakeUp(byte[] macAddress)
        {
            Connect(IPAddress.Broadcast, 40000);
            SetSocketOptions();

            var packet = BuildPacket(macAddress);
            Send(packet, packet.Length);
        }

        private static byte[] BuildPacket(byte[] mac)
        {
            // WOL packet contains a 6-bytes trailer and 16 times a 6-bytes sequence containing the MAC address.
            var packet = new byte[17 * 6];

            // Trailer of 6 times 0xFF.
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            // Body of magic packet contains 16 times the MAC address.
            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];

            return packet;
        }

        private static byte[] ParseMac(string macAddress)
        {
            var macstr = Regex.Replace(macAddress, "[^0-9a-fA-F]", "");
            if (macstr.Length != 12) throw new ArgumentException($"invalid mac address {macAddress}", nameof(macAddress));
            var byteIndex = 0;
            var array = new byte[6];
            for (int k = 0; k < 6; k++)
            {
                array[k] = byte.Parse(macstr.Substring(byteIndex, 2), NumberStyles.HexNumber);
                byteIndex += 2;
            }
            return array;
        }
    }
}