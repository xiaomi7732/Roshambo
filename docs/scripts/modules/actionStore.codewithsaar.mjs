class ActionStore {
    constructor() {
        this.actions = [];
    }

    getActionByKey(key) {
        const targets = this.actions.filter(a => a.key === key);
        if (targets.length !== 1) {
            throw `Unexpected count of actions returned by key ${key}. Expect: 1, actual: ${targets.length}`;
        }
        return targets[0];
    }
}

export const instance = new ActionStore();
