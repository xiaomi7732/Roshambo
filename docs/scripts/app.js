// const backendBaseUrl = "https://localhost:7255";
const backendBaseUrl = "https://roshamboapp.azurewebsites.net";
const USER_ID = "user-id";

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

window.addEventListener("load", async () => {
    try {
        let userId = localStorage.getItem(USER_ID);

        try {
            if (!userId) {
                // First time user
                console.log("First time player. Requesting a user id.");
                const userIdResponse = await fetch(backendBaseUrl + "/");
                const playerInfo = await userIdResponse.json();
                userId = playerInfo.suggestedUserId.value;
                localStorage.setItem(USER_ID, userId);
            }
            else {
                console.log("Existing player.");
            }
        } catch (ex) {
            console.error("Failed getting user id from the backend. Please contact the developer.");
        }

        const playerUrl = `${backendBaseUrl}/players/${userId}`;
        const playerResponse = await fetch(playerUrl);

        const stat = await playerResponse.json();
        console.log(JSON.stringify(stat));
        generateNextMoves(stat.actions.filter(a => a.rel === "action"));

        humanWinningElement.innerText = stat.statistics.humanWinning;
        computerWinningElement.innerText = stat.statistics.computerWinning;
        drawElement.innerText = stat.statistics.draw;

        userHumanWinningElement.innerText = stat.userStatistics.humanWinning;
        userComputerWinningElement.innerText = stat.userStatistics.computerWinning;
        userDrawElement.innerText = stat.userStatistics.draw;

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
    const postResponse = await fetch(`${backendBaseUrl}${action.href}`, {
        method: action.method,
        body: JSON.stringify({
            "userId": localStorage.getItem(USER_ID),
        }),
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

    userHumanWinningElement.innerText = body.userStatistics.humanWinning;
    userComputerWinningElement.innerText = body.userStatistics.computerWinning;
    userDrawElement.innerText = body.userStatistics.draw;
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
