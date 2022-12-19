// const backendBaseUrl = "http://localhost:5295";
const backendBaseUrl = "https://roshamboapp.azurewebsites.net";

const humanWinningElement = document.getElementById("humanWinCount");
const computerWinningElement = document.getElementById("computerWinCount");
const drawElement = document.getElementById("draw");
const computerMoveImg = document.getElementById("computerMoveImg");
const resetButton = document.getElementById("reset");
const roundResultElement = document.getElementById("roundResult");

const DYNAMIC_MOVES = "dynamicMoves";

let stopAt = undefined;
let computerMoveCursor = 0;
let rotationStopper = -1;

const resources =
{
    "rock": {
        "displayIndex": 10,
        "img": "images/Rock.png",
        "gray_img": "images/Rock_Gray.png",
        "action": "paper",
    },
    "scissor": {
        "displayIndex": 20,
        "img": "images/Scissor.png",
        "gray_img": "images/Scissor_Gray.png",
        "action": "scissor",
    },
    "paper": {
        "displayIndex": 30,
        "img": "images/Paper.png",
        "gray_img": "images/Paper_Gray.png",
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
    const nextMoveContainerElement = document.getElementById(DYNAMIC_MOVES);
    actionList.forEach((item) => {
        let img = document.createElement('img');
        img.src = resources[item.name].img;
        img.className = "move-image user-move";
        img.setAttribute("key", item.name);
        img.alt = "An image for " + item.name;
        img.addEventListener("click", async () => {
            await goWithAsync(item);
        });

        nextMoveContainerElement.appendChild(img);
    });
}

function start() {
    highlightUserMove(null);
    roundResultElement.innerText = null;
    stopAt = undefined;
    rotateComputerMove();
}

function rotateComputerMove() {
    if (rotationStopper != -1) {
        clearInterval(rotationStopper);
        rotationStopper = -1;
    }

    rotationStopper = setInterval(() => {
        const resKeys = Object.keys(resources);
        const targetKey = resKeys[computerMoveCursor];
        const targetRes = resources[targetKey];

        computerMoveImg.src = targetRes.img;

        if (stopAt !== undefined && targetKey === stopAt) {
            clearInterval(rotationStopper);
            rotationStopper = -1;
        }
        computerMoveCursor = (computerMoveCursor + 1) % 3;
    }, 100);
}



async function goWithAsync(action) {
    const postResponse = await fetch(backendBaseUrl + action.href, {
        method: action.method,
        headers: {
            "Content-Type": "application/json"
        },
    })

    const body = await postResponse.json();
    console.log(JSON.stringify(body));

    highlightUserMove(null);
    highlightUserMove(action.name);

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

function highlightUserMove(action) {
    const nextMoveContainerElement = document.getElementById(DYNAMIC_MOVES);
    const imgElements = nextMoveContainerElement.getElementsByTagName("img");

    for (const imgElement of imgElements) {
        const key = imgElement.getAttribute('key');
        if (action === null) {
            imgElement.src = resources[key].img;
            continue;
        }

        if (key !== action) {
            imgElement.src = resources[key].gray_img;
        }
    }
}
