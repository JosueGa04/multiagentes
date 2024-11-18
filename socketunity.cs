using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketClient : MonoBehaviour
{
    void Start()
    {
        TcpClient client = new TcpClient("localhost", 65432);
        NetworkStream stream = client.GetStream();

        byte[] dataToSend = Encoding.ASCII.GetBytes("Hola desde Unity");
        stream.Write(dataToSend, 0, dataToSend.Length);

        byte[] receivedBuffer = new byte[1024];
        int bytes = stream.Read(receivedBuffer, 0, receivedBuffer.Length);
        string responseData = Encoding.ASCII.GetString(receivedBuffer, 0, bytes);

        Debug.Log("Recibido: " + responseData);

        stream.Close();
        client.Close();
    }
}