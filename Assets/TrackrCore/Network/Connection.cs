using UnityEngine;
// represents the state of a connection to another peer

public class Connection {
    
    byte state;
    CommandStream commandBuffer;
    //DataStream dataBuffer;

    public Connection(string hostname, ushort port) {

        commandBuffer = new CommandStream(hostname, port);
        //dataBuffer = new DataStream(hostname, port);

    }

//    public void connect(string remoteHostname, ushort port) {
//        commandBuffer.connect(remoteHostname, port);
//    }

    public bool sendCommand(string message) {

        return commandBuffer.write(message);

    }

    public string receiveCommand() {

        return commandBuffer.read();

    }
    /*
    public bool sendData(string message) {

        return dataBuffer.write(message);

    }

    public string receiveData() {
        return dataBuffer.read();
    }
    */
}