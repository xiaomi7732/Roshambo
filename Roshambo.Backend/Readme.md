# Roshambo Backend Spec

## What does the API do?

* Post an available option, return the result.
* The result incudes the info for win or lose, what's the user's choice, and what's the computer's choice.
* Compute statistics for the winning rate of computer vs human
* Good to have: compute winning rate for the current user.


## Mapping

0 - Rock
1 - Paper
2 - Scissor

## Actions

The client choose to post 1 of the following action

* GET /

    Displays statistics of human vs computer.

    * Example

        `application/json`:

        ```jsonc
        {
            "human-winning": 123,
            "computer-winning": 112
        }
        ```

* POST
    * /rounds/rock
    * /rounds/paper
    * /rounds/scissor

    * Result example

        * `application/json`

        ```jsonc
        {
            "result": "win",
            "human-win-round": true,
            "human-wining": 124,
            "computer-wining": 112
        }
        ```
