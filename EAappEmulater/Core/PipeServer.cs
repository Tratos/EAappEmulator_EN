﻿using EAappEmulater.Enums;
using EAappEmulater.Helper;

namespace EAappEmulater.Core;

public class PipeServer
{
    private readonly NamedPipeClientStream _pipeClient;
    private readonly BattlelogType _battleType;

    private bool _isRunning = true;
    private readonly Thread _thread;

    public string GameState { get; set; } = null;

    public PipeServer(BattlelogType battleType)
    {
        this._battleType = battleType;

        _pipeClient = battleType switch
        {
            BattlelogType.BF3 => new NamedPipeClientStream(".", "venice_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF4 => new NamedPipeClientStream(".", "warsaw_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF4Debug => new NamedPipeClientStream(".", "warsaw_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BFH => new NamedPipeClientStream(".", "omaha_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BF1 => new NamedPipeClientStream(".", "tunguska_snowroller", PipeDirection.InOut, PipeOptions.None),
            BattlelogType.BFV => new NamedPipeClientStream(".", "tunguska_snowroller", PipeDirection.InOut, PipeOptions.None),
            _ => new NamedPipeClientStream(".", "venice_snowroller", PipeDirection.InOut, PipeOptions.None),
        };

        _thread = new Thread(PipeHandlerThread)
        {
            Name = "PipeHandlerThread",
            IsBackground = true
        };
        _thread.Start();

        LoggerHelper.Info($"{_battleType} Thread status {_thread.ThreadState}");
        LoggerHelper.Info($"{_battleType} Started Pipe listening service successfully");
    }

    /// <summary>
    /// Destroy the Pipe listening service
    /// </summary>
    public void Dispose()
    {
        _isRunning = false;
        _pipeClient.Close();

        LoggerHelper.Info($"{_battleType} Thread status {_thread.ThreadState}");
        LoggerHelper.Info($"{_battleType} Stopped Pipe listening service successfully");
    }

    /// <summary>
    /// Pipe pipeline processing thread
    /// </summary>
    private async void PipeHandlerThread()
    {
        try
        {
            var array = new byte[256];
            using var binaryReader = new BinaryReader(_pipeClient);

            while (_isRunning)
            {
                try
                {
                    // Waiting to connect to the server
                    if (!_pipeClient.IsConnected)
                    {
                        // Timeout duration 3600 seconds
                        await _pipeClient.ConnectAsync(3600000);
                    }
                }
                catch (TimeoutException ex)
                {
                    LoggerHelper.Error($"{_battleType} Handle pipe client connection timeout exception", ex);
                    continue;
                }

                // Process the connected PipeStream object
                while (binaryReader.Read(array, 0, array.Length) != 0)
                {
                    var stringBuilder = new StringBuilder();
                    var memoryStream = new MemoryStream();

                    var buffer = array[4];

                    memoryStream.Write(array, 5, buffer);
                    stringBuilder.Append(Encoding.ASCII.GetString(memoryStream.ToArray()));

                    memoryStream = new MemoryStream();

                    memoryStream.Write(array, 7 + buffer, array[5 + buffer]);
                    stringBuilder.Append(" : " + Encoding.ASCII.GetString(memoryStream.ToArray()));

                    GameState = stringBuilder.ToString();
                    LoggerHelper.Debug($"{_battleType} Pipe current game state {stringBuilder}");
                }

                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"{_battleType} Handling an exception in Pipe client connection", ex);
        }
    }
}