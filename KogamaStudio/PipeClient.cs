
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KogamaStudio
{
    public static class PipeClient
    {
        public static void SendCommand(string cmd)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", "KogamaStudio_CSharp", PipeDirection.Out))
                    {
                        pipeClient.Connect(5000);
                        using (var writer = new StreamWriter(pipeClient))
                        {
                            writer.Write(cmd);
                            writer.Flush();
                        }
                    }

                }
                catch (Exception ex)
                {
                    KogamaStudio.Log.LogError($"[PipeClient] Error: {ex.Message}");
                }
            });
        }
    }
}
