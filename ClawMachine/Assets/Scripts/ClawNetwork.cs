using System;
using System.Collections.Concurrent;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;

/// <summary>
/// Socket.IO 서버에 연결해 폰 컨트롤러 입력을 받아 ClawController를 조작.
///
/// 흐름: 폰 버튼 → 서버 → (이 스크립트) → ClawController.MoveInput / Confirm
///
/// WebSocket 콜백은 메인 스레드 밖에서 오므로, 받은 입력을 큐에 쌓고
/// Update()에서 꺼내 처리한다. (Unity 트랜스폼 조작은 메인 스레드 전용)
/// </summary>
public class ClawNetwork : MonoBehaviour
{
    [Header("연결 설정")]
    [SerializeField] string serverUrl = "https://claw-machine-9pd6.onrender.com";
    [SerializeField] string machineId = "machine01"; // 폰 페이지의 MACHINE_ID와 같아야 함

    [Header("조작 대상")]
    [SerializeField] ClawMove claw;

    SocketIOUnity socket;

    // 메인 스레드로 넘길 입력 큐 (콜백 스레드 → Update)
    readonly ConcurrentQueue<InputMsg> inbox = new ConcurrentQueue<InputMsg>();

    // 현재 눌려 있는 방향 (버튼 누르는 동안 계속 이동시키기 위해)
    Vector2 currentDir;

    struct InputMsg
    {
        public string cmd;     // up / down / left / right / confirm
        public string action;  // down(누름) / up(뗌)
    }

    void Start()
    {
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
        });

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("[ClawNetwork] 서버 연결됨");
            socket.Emit("join", machineId); // 폰과 같은 방에 입장
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("[ClawNetwork] 연결 끊김");
            inbox.Enqueue(new InputMsg { cmd = "all", action = "up" }); // 안전: 이동 정지
        };

        // 서버가 보내는 "input" 이벤트 수신
        socket.On("input", response =>
        {
            // 콜백 스레드에서 실행됨 → 파싱만 하고 큐에 적재
            var data = response.GetValue<InputData>(0);
            Debug.Log($"[ClawNetwork] 파싱됨: cmd={data.cmd}, action={data.action}");
            inbox.Enqueue(new InputMsg { cmd = data.cmd, action = data.action });
        });

        socket.Connect();
    }

    void Update()
    {
        // 도착한 입력을 메인 스레드에서 처리
        while (inbox.TryDequeue(out InputMsg msg))
        {
            HandleInput(msg);
        }

        // 눌려 있는 방향으로 계속 이동 (키보드 입력과 동일한 느낌)
        if (currentDir.sqrMagnitude > 0.01f)
        {
            claw.MoveInput(currentDir);
        }
        else
            claw.MoveInput(Vector2.zero);
    }

    void HandleInput(InputMsg msg)
    {
        bool pressed = msg.action == "down";

        switch (msg.cmd)
        {
            case "up": currentDir = pressed ? new Vector2(0f, 1f) : ClearAxis(currentDir, 'y'); break;
            case "down": currentDir = pressed ? new Vector2(0f, -1f) : ClearAxis(currentDir, 'y'); break;
            case "left": currentDir = pressed ? new Vector2(-1f, 0f) : ClearAxis(currentDir, 'x'); break;
            case "right": currentDir = pressed ? new Vector2(1f, 0f) : ClearAxis(currentDir, 'x'); break;

            case "confirm":
                if (pressed) claw.Confirm();
                break;
            case "release":
                if (pressed) claw.Release();
                break;

            case "all": // 연결 끊김 등 비상 정지
                currentDir = Vector2.zero;
                break;
        }
    }

    // 한 축 버튼만 떼었을 때 그 축만 0으로 (다른 축 입력은 유지)
    Vector2 ClearAxis(Vector2 dir, char axis)
    {
        if (axis == 'x') dir.x = 0f;
        else dir.y = 0f;
        return dir;
    }

    void OnApplicationQuit()
    {
        socket?.Disconnect();
    }

    [Serializable]
    class InputData
    {
        [JsonProperty("machineId")]
        public string machineId { get; set; }

        [JsonProperty("cmd")]
        public string cmd { get; set; }

        [JsonProperty("action")]
        public string action { get; set; }
    }
}