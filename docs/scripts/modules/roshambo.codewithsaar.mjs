import { instance as actionStore } from './actionStore.codewithsaar.mjs'

const USER_ID = "user-id"

export class Roshambo {
    constructor(backendUrl) {
        this.backendUrl = backendUrl;
    }

    async signIn() {
        const serverHandShake = await this.#handshakeServer();  // local storage will be updated with the latest user id.
        const userId = this.#getExistUserId(USER_ID);

        if (userId === null || userId === undefined) {
            throw 'Failed signing player in. Please contact the developer';
        }

        const playerSignInUrl = actionStore.actions[0].href.replace('{uid}', userId);
        console.log('Player signing in at: ' + playerSignInUrl);

        const playerSignInResponse = await fetch(playerSignInUrl);
        const playerStatus = await playerSignInResponse.json();
        actionStore.actions = playerStatus.actions;
        return playerStatus;
    }

    async execute(key) {
        const action = actionStore.getActionByKey(key);
        const goResponse = await fetch(action.href, {
            method: action.method,
            body: JSON.stringify({
                userId: localStorage.getItem(USER_ID),
            }),
            headers: {
                "Content-Type": "application/json"
            },
        });

        const result = await goResponse.json();
        actionStore.actions = result.actions;
        return result;
    }

    #getExistUserId(key) {
        return localStorage.getItem(key);
    }

    async #handshakeServer() {
        try {
            const userIdResponse = await fetch(this.backendUrl + "/");
            const playerInfo = await userIdResponse.json();

            actionStore.actions = playerInfo.next;

            if (localStorage.getItem(USER_ID) === null) {
                const userId = playerInfo.suggestedUserId.value;
                localStorage.setItem(USER_ID, userId);
            }
            return playerInfo;
        } catch (ex) {
            throw `Failed creating new user. Details: ` + ex;
        }
    }
}