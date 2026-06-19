using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class RoomCodeUtility
{
    private const int RoomPortBase = 10000;

    public static string GetLocalRoomCode(ushort port)
    {
        int code = port - RoomPortBase;

        if (code >= 1000 && code <= 9999)
            return code.ToString("0000");

        return "7777";
    }

    public static string GenerateRoomCode()
    {
        return UnityEngine.Random.Range(1000, 10000).ToString("0000");
    }

    public static ushort PortFromFourDigitCode(string code)
    {
        if (!int.TryParse(code, out int number))
            return 17777;

        number = Mathf.Clamp(number, 1000, 9999);
        return (ushort)(RoomPortBase + number);
    }

    public static bool TryDecode(string code, out string address, out ushort port)
    {
        address = "127.0.0.1";
        port = 7777;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        string cleanCode = code.Trim().Replace("-", "").ToUpperInvariant();

        if (cleanCode.Equals("LOCAL", StringComparison.OrdinalIgnoreCase))
            return true;

        if (cleanCode.Length == 4 && cleanCode.All(char.IsDigit))
        {
            port = PortFromFourDigitCode(cleanCode);
            return true;
        }

        if (IPAddress.TryParse(cleanCode, out IPAddress directIp))
        {
            address = directIp.ToString();
            return true;
        }

        if (cleanCode.Length != 12)
            return false;

        try
        {
            byte[] bytes = new byte[6];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(cleanCode.Substring(i * 2, 2), 16);
            }

            address = string.Join(".", bytes.Take(4));
            port = (ushort)((bytes[4] << 8) | bytes[5]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string FormatCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code) && code.Length == 4)
            return code;

        if (string.IsNullOrWhiteSpace(code) || code.Length != 12)
            return code;

        return code.Substring(0, 4) + "-" + code.Substring(4, 4) + "-" + code.Substring(8, 4);
    }

    private static string Encode(string address, ushort port)
    {
        string[] parts = address.Split('.');

        if (parts.Length != 4)
            return "LOCAL";

        byte[] bytes = new byte[6];

        for (int i = 0; i < 4; i++)
        {
            bytes[i] = byte.Parse(parts[i]);
        }

        bytes[4] = (byte)(port >> 8);
        bytes[5] = (byte)(port & 0xff);

        return string.Concat(bytes.Select(value => value.ToString("X2")));
    }

    private static string GetLocalIPv4Address()
    {
        try
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addresses[i]))
                {
                    return addresses[i].ToString();
                }
            }
        }
        catch (SocketException exception)
        {
            Debug.LogWarning("Cannot get LAN IP for room code: " + exception.Message);
        }

        return "127.0.0.1";
    }
}
