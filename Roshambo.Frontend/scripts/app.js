const backendBaseUrl = "http://localhost:5295";

const humanWinningElement = document.getElementById("humanWinCount");
const computerWinningElement = document.getElementById("computerWinCount");
const drawElement = document.getElementById("draw");
const computerMoveImg = document.getElementById("computerMoveImg");
const resetButton = document.getElementById("reset");
const roundResultElement = document.getElementById("roundResult");

let stopAt = undefined;
let computerMoveCursor = 0;

const resources =
{
    "rock": {
        "displayIndex": 10,
        "img": "images/Rock.png",
        "action": "paper",
    },
    "scissor": {
        "displayIndex": 20,
        "img": "images/Scissor.png",
        "action": "scissor",
    },
    "paper": {
        "displayIndex": 30,
        "img": "images/Paper.png",
        "action": "paper",
    },
};

window.addEventListener("load", async () => {
    try {
        let statResponse = await fetch(backendBaseUrl + "/");
        const stat = await statResponse.json();
        console.log(JSON.stringify(stat));
        generateNextMoves(stat.actions.filter(a => a.rel === "action"));
        humanWinningElement.innerText = stat.statistics.humanWinning;
        computerWinningElement.innerText = stat.statistics.computerWinning;
        drawElement.innerText = stat.statistics.draw;

        resetButton.addEventListener("click", () => {
            start();
        });

        start();
    } catch (ex) {
        alert("Error contacting backend at: " + backendBaseUrl + ex);
        return;
    }
});

function generateNextMoves(actionList) {
    const nextMoveContainerElement = document.getElementById("dynamicMoves");
    actionList.forEach((item) => {
        let img = document.createElement('img');
        img.src = resources[item.name].img;
        img.addEventListener("click", async () => {
            await goWithAsync(item.name);
        });

        nextMoveContainerElement.appendChild(img);
    });
}

function start() {
    stopAt = undefined;
    rotateComputerMove();
}

function rotateComputerMove() {
    var intervalId = setInterval(() => {
        console.log(`Interval Id: ${intervalId}`);

        const resKeys = Object.keys(resources);
        const targetKey = resKeys[computerMoveCursor];
        const targetRes = resources[targetKey];

        computerMoveImg.src = targetRes.img;

        if (stopAt !== undefined && targetKey === stopAt) {
            clearInterval(intervalId);
        }
        computerMoveCursor = (computerMoveCursor + 1) % 3;
    }, 100);
}

async function goWithAsync(action) {
    const postResponse = await fetch(backendBaseUrl + "/rounds/" + action, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
    })

    const body = await postResponse.json();
    console.log(JSON.stringify(body));

    const computerMoveName = body.round.computerMove.name;
    stopAt = computerMoveName;

    const roundResult = body.round.result;

    const computerMoveResource = resources[computerMoveName];

    let resultText = "Unknown";
    switch (roundResult) {
        case 0:
            resultText = "You Win!"
            break;
        case 1:
            resultText = "You lose!";
            break;
        case 2: resultText = "Draw!";
            break;
        default:
            resultText = "Error!";
    }

    roundResultElement.innerText = resultText;

    // TODO: Handle -1
    setTimeout(() => {
        computerMoveImg.src = computerMoveResource.img;
    }, 100);

    // TODO: Reuse
    humanWinningElement.innerText = body.statistics.humanWinning;
    computerWinningElement.innerText = body.statistics.computerWinning;
    drawElement.innerText = body.statistics.draw;
}
