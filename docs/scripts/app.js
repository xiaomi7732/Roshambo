import { Roshambo } from './modules/roshambo.codewithsaar.mjs';
import * as ActionStore from './modules/actionStore.codewithsaar.mjs'

// const backendBaseUrl = "https://localhost:7255";
const backendBaseUrl = "https://roshamboapp.azurewebsites.net";
const roshamboAPI = new Roshambo(backendBaseUrl);

const humanWinningElement = document.getElementById("humanWinCount");
const computerWinningElement = document.getElementById("computerWinCount");
const drawElement = document.getElementById("draw");

const userHumanWinningElement = document.getElementById("userHumanWinCount");
const userComputerWinningElement = document.getElementById("userComputerWinCount");
const userDrawElement = document.getElementById("userDraw");

const computerMoveImg = document.getElementById("computerMoveImg");
const resetButton = document.getElementById("reset");
const roundResultElement = document.getElementById("roundResult");

const splashElement = document.getElementById("splash");
const pageContentElement = document.getElementById("page-content");

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

const resultComments = {
    "computerWinningWorld": {
        "text": "The computer is winning the world!"
    },
    "playerWinningWorld": {
        "text": "We collectively are winning the world, stay chill!"
    },
    "playerWinning": {
        "text": "You are showing the computer who's the lord."
    },
    "computerWinning": {
        "text": "The computer is beating someone up."
    }
}

window.addEventListener("load", async () => {
    try {
        const stat = await roshamboAPI.signIn();
        console.log(JSON.stringify(stat));
        generateNextMoves(ActionStore.instance.actions.filter(a => a.rel === "ready"));

        updateStatistics(stat.statistics, stat.userStatistics);

        resetButton.addEventListener("click", () => {
            start();
        });

        splashElement.style.display = "none";
        pageContentElement.className = "page-content";

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
        img.src = resources[item.key].img;
        img.className = "move-image user-move";
        img.setAttribute("key", item.key);
        img.alt = "An image for " + item.key;
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
    const body = await roshamboAPI.execute(action.key);
    console.log(JSON.stringify(body));

    highlightUserMove(null);
    highlightUserMove(action.key);

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

    updateStatistics(body.statistics, body.userStatistics);

    startTimer(5);
}

function startTimer(invokeOnSeconds) {
    const autoRestartPromptElement = document.getElementById("autoRestartPrompt");
    autoRestartPromptElement.innerText = `Auto start another round in ${invokeOnSeconds} seconds`;
    let timer = setInterval(() => {
        autoRestartPromptElement.innerText = `Auto start another round in ${invokeOnSeconds} seconds`;
        if (invokeOnSeconds-- === 0) {
            autoRestartPromptElement.innerText = null;
            start();
            clearInterval(timer);
        }
    }, 500);
}

function updateStatistics(statistics, userStatistics) {
    humanWinningElement.innerText = statistics.humanWinning;
    computerWinningElement.innerText = statistics.computerWinning;
    drawElement.innerText = statistics.draw;

    userHumanWinningElement.innerText = userStatistics.humanWinning;
    userComputerWinningElement.innerText = userStatistics.computerWinning;
    userDrawElement.innerText = userStatistics.draw;

    updateWorldBasedComment(statistics.computerWinning, statistics.humanWinning);
    updatePlayerBasedComment(userStatistics.computerWinning, userStatistics.humanWinning);
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

function updateWorldBasedComment(computerWinning, humanWinning) {
    const targetElement = document.getElementById("worldBasedComment");
    if (computerWinning >= humanWinning) {
        targetElement.innerText = resultComments.computerWinningWorld.text;
    }
    else {
        targetElement.innerText = resultComments.playerWinningWorld.text;
    }
}

function updatePlayerBasedComment(computerWinning, humanWinning) {
    const targetElement = document.getElementById("playerBasedComment");
    if (computerWinning >= humanWinning) {
        targetElement.innerText = resultComments.computerWinning.text;
    }
    else {
        targetElement.innerText = resultComments.playerWinning.text;
    }
}
