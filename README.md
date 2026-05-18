# Slot Machine — Unity WebGL Game

---

## Game Overview

A classic 3-reel slot machine built in Unity.
Player selects a bet (10, 50, or 100 coins), pulls the lever, and the reels spin.
Results are pre-determined via weighted RNG before animation starts.
Match the same symbol in the centre row of all 3 reels to win.

| Symbol | Payout |
|--------|--------|
| Cherry | 2x     |
| Bell   | 5x     |
| Bar    | 10x    |
| Seven  | 25x    |

---

## How to Run the WebGL Build

1. Clone the repository
2. Open the `Build/WebGL` folder
3. Run a local server :

```bash
cd Build/WebGL
python -m http.server 8000
```
Then open `http://localhost:8000` in your browser.
---

## Bonus Features

- **Weighted RNG** — each symbol has its own weight controlling how often it appears
- **Audio system** — BG music, reel spin loop, jackpot fanfare, lose sting, UI hover and click
- **Fade animations** — win and game over popups fade in and out smoothly
- **Game over flow** — detects zero balance, shows popup, play again resets the game

---

## Thought Process

Results are pre-determined via weighted RNG before the spin animation begins — keeping the game fair while giving full control over outcome frequency through symbol weights.
Each system is kept isolated: `RNGSystem` handles only random selection, `ReelController` handles only its own visual spin and snap, `SlotMachine` orchestrates all 3 reels and waits for all to fully stop before evaluating, and `GameManager` owns all UI state and balance logic.
The animation is purely cosmetic - the outcome is always decided first.