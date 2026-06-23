// ===== 인형뽑기 테스트 서버 =====
// 역할: 폰(컨트롤러)에서 온 입력을 Unity로 넘겨주는 우체국.
// 같은 와이파이 안에서 폰과 PC를 연결합니다.

const http = require("http");
const fs = require("fs");
const path = require("path");

// (1) 웹서버: 폰이 접속하면 controller.html을 보내줌
const server = http.createServer((req, res) => {
    // 어떤 주소로 들어와도 컨트롤러 페이지를 보여줌
    const filePath = path.join(__dirname, "controller.html");
    fs.readFile(filePath, (err, data) => {
        if (err) {
            res.writeHead(500);
            res.end("controller.html 파일을 찾을 수 없어요");
            return;
        }
        res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
        res.end(data);
    });
});

// (2) WebSocket: 폰/Unity와 끊기지 않는 연결을 맺음
const { Server } = require("socket.io");
const io = new Server(server, {
    cors: { origin: "*" }, // 테스트라 모두 허용
});

io.on("connection", (socket) => {
    console.log("누군가 접속함:", socket.id);

    // 접속자가 어느 기계 방에 들어갈지 (폰도 Unity도 같은 방으로)
    socket.on("join", (machineId) => {
        socket.join(machineId);
        console.log(`${socket.id} → ${machineId} 방 입장`);
    });

    // 컨트롤러 입력을 같은 방의 다른 접속자(Unity)에게 전달
    socket.on("input", (data) => {
        console.log("입력 받음:", data); // 폰→서버가 되는지 눈으로 확인용
        socket.to(data.machineId).emit("input", data);
    });

    socket.on("disconnect", () => {
        console.log("접속 끊김:", socket.id);
    });
});

// (3) 서버 켜기
const PORT = 3000;
server.listen(PORT, () => {
    console.log("====================================");
    console.log(` 서버 켜짐! 포트 ${PORT}`);
    console.log(" 폰에서 접속할 주소는 잠시 후 안내할게요");
    console.log("====================================");
});