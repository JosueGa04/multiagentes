import socket

# Configura el socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.bind(('localhost', 65432))
sock.listen()

print("Esperando conexión...")
conn, addr = sock.accept()

with conn:
    print('Conectado por', addr)
    while True:
        data = conn.recv(1024)
        if not data:
            break
        print('Recibido:', data.decode())
        conn.sendall(b'Datos recibidos')

sock.close()