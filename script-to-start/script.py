import socket
import subprocess
import threading
import time
import os
import signal
import shlex

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SERVER_DLL = os.path.join(SCRIPT_DIR, "../memoryDb/out/memoryDb.dll")
if not os.path.isfile(SERVER_DLL):
    raise FileNotFoundError(f"Server executable not found at path: {SERVER_DLL}")

SERVER_PATH = "dotnet"

MASTER_PORT = 6379
REPLICA_PORT = 6380


def wait_for_server(host, port, timeout=5):
    start = time.time()
    while time.time() - start < timeout:
        try:
            with socket.create_connection((host, port), timeout=0.5):
                return True
        except (ConnectionRefusedError, OSError):
            time.sleep(0.2)
    raise TimeoutError(f"Server not available on {host}:{port}")


def read_line(sock):
    line = b""
    while not line.endswith(b"\r\n"):
        part = sock.recv(1)
        if not part:
            raise ConnectionError("Socket closed")
        line += part
    return line[:-2]


def read_bulk_string(sock):
    length = int(read_line(sock))

    if length == -1:
        return "null"
    data = b""
    while len(data) < length + 2:
        data += sock.recv(length + 2 - len(data))
    return data[:-2].decode()


def read_array(sock):
    count = int(read_line(sock))
    return [read_response(sock) for _ in range(count)]


def read_response(sock):
    prefix = sock.recv(1)
    if not prefix:
        raise ConnectionError("Socket closed")

    if prefix == b"$":
        return read_bulk_string(sock)
    elif prefix == b"*":
        return read_array(sock)
    elif prefix == b":":
        return int(read_line(sock))
    elif prefix == b"+":
        return read_line(sock).decode()
    elif prefix == b"-":
        return "ERR: " + read_line(sock).decode()
    else:
        return f"Unknown prefix: {prefix}"


def send_command(sock, command):
    parts = shlex.split(command.strip())
    resp = f"*{len(parts)}\r\n"
    for part in parts:
        resp += f"${len(part)}\r\n{part}\r\n"
    sock.sendall(resp.encode())
    return read_response(sock)


def psubscribe_and_listen(port, pattern, results):
    with socket.create_connection(("127.0.0.1", port)) as sock:
        print("AUTHENTICATING (pattern)...")
        print(send_command(sock, "AUTH your_password"))

        print("PSUBSCRIBING...", send_command(sock, f"PSUBSCRIBE {pattern}"))

        try:
            for _ in range(2):
                data = sock.recv(1024)
                results.append(data.decode())
                print("PATTERN MESSAGE RECEIVED:")
                print(data.decode())
        except socket.timeout:
            print("No pattern message received (timeout)")

        print("PUNSUBSCRIBING...", send_command(sock, f"PUNSUBSCRIBE {pattern}"))


def print_nested(data, indent=0):
    if isinstance(data, list):
        for item in data:
            print_nested(item, indent + 2)
    else:
        print(" " * indent + str(data))


def stream_logs(stream, prefix=""):
    for line in iter(stream.readline, b""):
        print(f"{prefix}{line.decode().rstrip()}")


def start_server(port, extra_args=None):
    args = [SERVER_DLL, "--port", str(port), "--dbfilename", f"dump{port}.rdb"]
    if extra_args:
        args.extend(extra_args)

    proc = subprocess.Popen(
        [SERVER_PATH] + args,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        creationflags=subprocess.CREATE_NEW_PROCESS_GROUP,
    )
    threading.Thread(
        target=stream_logs, args=(proc.stdout, f"[{port} STDOUT] "), daemon=True
    ).start()
    threading.Thread(
        target=stream_logs, args=(proc.stderr, f"[{port} STDERR] "), daemon=True
    ).start()
    return proc


def subscribe_and_listen(port, channel, results):
    with socket.create_connection(("127.0.0.1", port)) as sock:
        print("AUTHENTICATING...")
        print(send_command(sock, "AUTH your_password"))

        print("SUBSCRIBING...", send_command(sock, f"SUBSCRIBE {channel}"))

        try:
            data = sock.recv(1024)
            results.append(data.decode())
            print("RECEIVED MESSAGE:")
            print(data.decode())
        except socket.timeout:
            print("No message received (timeout)")

        print("UNSUBSCRIBING...", send_command(sock, f"UNSUBSCRIBE {channel}"))

def runTestForScripts(master_sock, ):
    script = """
    return (function()
    redis.call('set', KEYS[1], ARGV[1])
    redis.call('incrby', KEYS[1], ARGV[2])
    redis.call('incrby', KEYS[1], ARGV[3])
    redis.call('incrby', KEYS[1], ARGV[4])
    return redis.call('get', KEYS[1])
    end)()
    """

    resp = send_command(master_sock, f"SCRIPT LOAD \"{script}\"")
    script_sha = resp.strip()  
    print("SCRIPT LOAD", script_sha)
    exists_resp = send_command(master_sock, f"SCRIPT EXISTS {script_sha}")
    print("SCRIPT EXISTS", exists_resp)

    key = "lua-test-key"


    evalsha_cmd = f"EVALSHA {script_sha} 1 {key} 5 10 15 20"
    result = send_command(master_sock, evalsha_cmd)
    print("EVALSHA result", result) 

    print("GET key after EVALSHA", send_command(master_sock, f"GET {key}"))  

    print("FLUSHALL", send_command(master_sock, "FLUSHALL"))

    result_post_flush = send_command(master_sock, evalsha_cmd)
    print("EVALSHA after FLUSHALL", result_post_flush)  # 

def test_zadd_zscore(master_sock):
    print("ZADD myzset 10.5 member1 →", send_command(master_sock, "ZADD myzset 10.5 member1"))
    print("ZADD myzset 5 member2 →", send_command(master_sock, "ZADD myzset 5 member2"))
    print("ZADD myzset 20.1 member1 →", send_command(master_sock, "ZADD myzset 20.1 member1"))  # update score

    print("ZSCORE myzset member1 →", send_command(master_sock, "ZSCORE myzset member1"))  # should be 20.1
    print("ZSCORE myzset member2 →", send_command(master_sock, "ZSCORE myzset member2"))  # should be 5
    print("ZSCORE myzset unknown →", send_command(master_sock, "ZSCORE myzset unknown"))  # should be nil

    print("ZINCRBY myzset 4.9 member2 →", send_command(master_sock, "ZINCRBY myzset 4.9 member2"))  # should be 9.9
    print("ZSCORE myzset member2 →", send_command(master_sock, "ZSCORE myzset member2"))  # should now be 9.9

    print("ZREM myzset member1 →", send_command(master_sock, "ZREM myzset member1"))  # should return :1
    print("ZREM myzset unknown →", send_command(master_sock, "ZREM myzset unknown"))  # should return :0
    print("ZSCORE myzset member1 →", send_command(master_sock, "ZSCORE myzset member1"))  # should be nil

    # Add more members for range testing
    print("ZADD myzset 1 memberA →", send_command(master_sock, "ZADD myzset 1 memberA"))
    print("ZADD myzset 3 memberB →", send_command(master_sock, "ZADD myzset 3 memberB"))
    print("ZADD myzset 6 memberC →", send_command(master_sock, "ZADD myzset 6 memberC"))

    print("ZSCORE myzset memberA →", send_command(master_sock, "ZSCORE myzset memberA"))  # should be 1
    print("ZSCORE myzset memberB →", send_command(master_sock, "ZSCORE myzset memberB"))  # should be 3
    print("ZSCORE myzset memberC →", send_command(master_sock, "ZSCORE myzset memberC"))  # should be 6

    # Remove members by score range
    print("ZREMRANGEBYSCORE myzset 1 5 →", send_command(master_sock, "ZREMRANGEBYSCORE myzset 1 5"))  # should remove A and B
    print("ZSCORE myzset memberA →", send_command(master_sock, "ZSCORE myzset memberA"))  # should be nil
    print("ZSCORE myzset memberB →", send_command(master_sock, "ZSCORE myzset memberB"))  # should be nil
    print("ZSCORE myzset memberC →", send_command(master_sock, "ZSCORE myzset memberC"))  # should still be 6

    # Edge case: empty removal
    print("ZREMRANGEBYSCORE myzset 100 200 →", send_command(master_sock, "ZREMRANGEBYSCORE myzset 100 200"))  # should return :0

    print("ZADD myzset 2 memberX →", send_command(master_sock, "ZADD myzset 2 memberX"))
    print("ZADD myzset 4 memberY →", send_command(master_sock, "ZADD myzset 4 memberY"))
    print("ZADD myzset 7 memberZ →", send_command(master_sock, "ZADD myzset 7 memberZ"))

    # Confirm scores
    print("ZSCORE myzset memberX →", send_command(master_sock, "ZSCORE myzset memberX"))
    print("ZSCORE myzset memberY →", send_command(master_sock, "ZSCORE myzset memberY"))
    print("ZSCORE myzset memberZ →", send_command(master_sock, "ZSCORE myzset memberZ"))

    # Remove by rank (rank 0 is lowest score)
    print("ZREMRANGEBYRANK myzset 0 1 →", send_command(master_sock, "ZREMRANGEBYRANK myzset 0 1"))  # Should remove 2 lowest

    # Confirm removals
    print("ZSCORE myzset memberA →", send_command(master_sock, "ZSCORE myzset memberA"))  # Possibly already removed by score
    print("ZSCORE myzset memberX →", send_command(master_sock, "ZSCORE myzset memberX"))  # Should be nil
    print("ZSCORE myzset memberY →", send_command(master_sock, "ZSCORE myzset memberY"))  # Should be present or not depending on rank

    # Edge case: out-of-range
    print("ZREMRANGEBYRANK myzset 100 200 →", send_command(master_sock, "ZREMRANGEBYRANK myzset 100 200"))  # Should return :0


def run():
    procs = []
    try:
        print("Starting master server...")
        master_proc = start_server(MASTER_PORT, ["--authpass", "your_password"])
        procs.append(master_proc)

        print("Starting replica server...")
        replica_proc = start_server(
            REPLICA_PORT,
            ["--replicaof", "127.0.0.1 6379", "--authpass", "your_password"],
        )
        procs.append(replica_proc)

        wait_for_server("127.0.0.1", MASTER_PORT)
        wait_for_server("127.0.0.1", REPLICA_PORT)
        time.sleep(1.0)

        subscription_results = []
        listener_thread = threading.Thread(
            target=subscribe_and_listen,
            args=(MASTER_PORT, "news", subscription_results),
        )
        listener_thread.start()

        psubscription_results = []
        pattern_listener_thread = threading.Thread(
            target=psubscribe_and_listen,
            args=(MASTER_PORT, "news*", psubscription_results),
        )
        pattern_listener_thread.start()

        # give it time to subscribe
        time.sleep(1)

        with socket.create_connection(("127.0.0.1", MASTER_PORT)) as master_sock:
            print(
                "SET key without auth →",
                send_command(master_sock, "SET key early_value"),
            )
            print(send_command(master_sock, "AUTH your_password"))

            print(
                "SET early_key early_value →",
                send_command(master_sock, "SET early_key early_value"),
            )
            print(
                "EVAL",
                send_command(
                    master_sock,
                    'EVAL "redis.call(\'set\', KEYS[1], ARGV[1]); redis.call(\'incrby\', KEYS[1], ARGV[2]); redis.call(\'incrby\', KEYS[1], ARGV[3]); redis.call(\'incrby\', KEYS[1], ARGV[4]); return redis.call(\'get\', KEYS[1])" 1 counter 5 10 15 20',
                ),
            )
            test_zadd_zscore(master_sock)
            runTestForScripts(master_sock)
            print("GET counter_key →", send_command(master_sock, "GET counter"))
            print("GET early_key →", send_command(master_sock, "GET early_key"))
            print("GET key that shouldnt exist →", send_command(master_sock, "GET key"))

            print(
                "XADD mystream * field1 value1 →",
                send_command(master_sock, "XADD mystream * field1 value1"),
            )
            print(
                "XADD mystream * field2 value2 →",
                send_command(master_sock, "XADD mystream * field2 value2"),
            )
            print(
                "XADD mystream * field3 value3 →",
                send_command(master_sock, "XADD mystream * field3 value3"),
            )

            print(
                "XRANGE mystream - + →",
                send_command(master_sock, "XRANGE mystream - +"),
            )

            print("LPUSH list1 a →", send_command(master_sock, "LPUSH list1 a"))
            print("LPUSH list1 b →", send_command(master_sock, "LPUSH list1 b"))
            print("LPUSH list1 c →", send_command(master_sock, "LPUSH list1 c"))
            print("RPUSH list1 d →", send_command(master_sock, "RPUSH list1 d"))
            print("RPUSH list1 e →", send_command(master_sock, "RPUSH list1 e"))
            print("RPUSH list1 f →", send_command(master_sock, "RPUSH list1 f"))

            print("LRANGE list1 0 -1 →", send_command(master_sock, "LRANGE list1 0 -1"))

            print("LPOP list1 →", send_command(master_sock, "LPOP list1"))
            print("RPOP list1 →", send_command(master_sock, "RPOP list1"))

            print("LRANGE list1 0 -1 →", send_command(master_sock, "LRANGE list1 0 -1"))

            # Different key to verify isolation
            print("LPUSH otherlist x →", send_command(master_sock, "LPUSH otherlist x"))
            print("RPUSH otherlist y →", send_command(master_sock, "RPUSH otherlist y"))
            print("LPUSH otherlist z →", send_command(master_sock, "LPUSH otherlist z"))

            print(
                "LRANGE otherlist 0 -1 →",
                send_command(master_sock, "LRANGE otherlist 0 -1"),
            )

            # Pop all elements to test underflow behavior
            print("LPOP otherlist →", send_command(master_sock, "LPOP otherlist"))
            print("LPOP otherlist →", send_command(master_sock, "LPOP otherlist"))
            print("LPOP otherlist →", send_command(master_sock, "LPOP otherlist"))
            print("LPOP otherlist →", send_command(master_sock, "LPOP otherlist"))

            # Length of lists
            print("LLEN list1 →", send_command(master_sock, "LLEN list1"))
            print("LLEN otherlist →", send_command(master_sock, "LLEN otherlist"))
            print("LLEN nonexistent →", send_command(master_sock, "LLEN nonexistent"))

            # Remove elements
            print("LPUSH list2 a →", send_command(master_sock, "LPUSH list2 a"))
            print("LPUSH list2 b →", send_command(master_sock, "LPUSH list2 b"))
            print("LPUSH list2 a →", send_command(master_sock, "LPUSH list2 a"))
            print("LPUSH list2 c →", send_command(master_sock, "LPUSH list2 c"))
            print("LPUSH list2 a →", send_command(master_sock, "LPUSH list2 a"))

            print("LRANGE list2 0 -1 →", send_command(master_sock, "LRANGE list2 0 -1"))

            # Remove 2 'a' from left to right
            print("LREM list2 2 a →", send_command(master_sock, "LREM list2 2 a"))
            print("LRANGE list2 0 -1 →", send_command(master_sock, "LRANGE list2 0 -1"))

            # Remove all remaining 'a'
            print("LREM list2 0 a →", send_command(master_sock, "LREM list2 0 a"))
            print("LRANGE list2 0 -1 →", send_command(master_sock, "LRANGE list2 0 -1"))

            # Add again to test negative count (right-to-left)
            print("RPUSH list2 a →", send_command(master_sock, "RPUSH list2 a"))
            print("RPUSH list2 a →", send_command(master_sock, "RPUSH list2 a"))
            print("LPUSH list2 a →", send_command(master_sock, "LPUSH list2 a"))
            print("LREM list2 -2 a →", send_command(master_sock, "LREM list2 -2 a"))
            print("LRANGE list2 0 -1 →", send_command(master_sock, "LRANGE list2 0 -1"))

            print(
                "PUBLISH news 'Breaking News!' →",
                send_command(master_sock, "PUBLISH news Breaking News!"),
            )
            print(
                "PUBLISH newsletter 'Monthly Update' →",
                send_command(master_sock, "PUBLISH newsletter 'Monthly Update'"),
            )
            print(
                "PUBLISH newscore 'Score Update' →",
                send_command(master_sock, "PUBLISH newscore 'Score Update'"),
            )
            print(
                "PUBLISH unrelated 'Ignore me' →",
                send_command(master_sock, "PUBLISH unrelated 'Ignore me'"),
            )

        listener_thread.join(timeout=5.0)
        if listener_thread.is_alive():
            print("Listener timed out waiting for message.")

        print("SUBSCRIBE/UNSUBSCRIBE test results:")
        for entry in subscription_results:
            print(entry.strip())

        time.sleep(1)
        with socket.create_connection(("127.0.0.1", REPLICA_PORT)) as replica_sock:
            print(
                "REPLICA GET early_key without auth →",
                send_command(replica_sock, "GET early_key"),
            )
            print(send_command(replica_sock, "AUTH your_password"))
            print(
                "REPLICA GET early_key →", send_command(replica_sock, "GET early_key")
            )
            print("REPLICA GET counter_key →", send_command(replica_sock, "GET counter"))
            print(
                "XRANGE from replica", send_command(replica_sock, "XRANGE mystream - +")
            )
            print(
                "LRANGE list1 0 -1 →", send_command(replica_sock, "LRANGE list1 0 -1")
            )
            print(
                "LRANGE otherlist 0 -1 →",
                send_command(replica_sock, "LRANGE otherlist 0 -1"),
            )
            print(
                "LRANGE FROM REPLICA list2 0 -1 →",
                send_command(replica_sock, "LRANGE list2 0 -1"),
            )

        print("Starting late-joining replica...")
        LATE_REPLICA_PORT = 6381
        late_replica_proc = start_server(
            LATE_REPLICA_PORT,
            ["--replicaof", "127.0.0.1 6379", "--authpass", "your_password"],
        )
        procs.append(late_replica_proc)

        pattern_listener_thread.join(timeout=5.0)
        if pattern_listener_thread.is_alive():
            print("Pattern listener timed out.")

        print("PSUBSCRIBE/PUNSUBSCRIBE test results:")
        for entry in psubscription_results:
            print(entry.strip())

        wait_for_server("127.0.0.1", LATE_REPLICA_PORT)
        time.sleep(4)

        with socket.create_connection(
            ("127.0.0.1", LATE_REPLICA_PORT)
        ) as late_replica_sock:
            print(
                "LATE REPLICA GET without auth early_key →",
                send_command(late_replica_sock, "GET early_key"),
            )
            print(send_command(late_replica_sock, "AUTH your_password"))
            print(
                "LATE REPLICA GET early_key →",
                send_command(late_replica_sock, "GET early_key"),
            )
            print(
                "LATE REPLICA GET none →",
                repr(send_command(late_replica_sock, "GET none")),
            )
            print(
                "XRANGE from LATE REPLICA",
                send_command(late_replica_sock, "XRANGE mystream - +"),
            )
            print(
                "LRANGE LATE REPLICA list1 0 -1 →",
                send_command(late_replica_sock, "LRANGE list1 0 -1"),
            )
            print(
                "LRANGE LATE REPLICA otherlist 0 -1 →",
                send_command(late_replica_sock, "LRANGE otherlist 0 -1"),
            )
            print(
                "LRANGE FROM LATE REPLICA list2 0 -1 →",
                send_command(late_replica_sock, "LRANGE list2 0 -1"),
            )

        time.sleep(1.5)

    finally:
        print("Shutting down servers...")
        for proc in procs:
            try:
                if os.name == "nt":
                    proc.send_signal(signal.CTRL_BREAK_EVENT)
                else:
                    proc.terminate()
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                print("Force killing server...")
                proc.kill()


if __name__ == "__main__":
    run()
