# Star Café: A Stress Relief Cooking Game


## Abstract

Young adults today face significant stress, often lacking accessible, engaging relief methods compatible with their digital lifestyles. Traditional stress management requires time, and many digital interactions, including some games, can increase anxiety. Addressing this, the "Star Café" project developed a relaxing cooking simulation game using Unity and C#. This game offers a unique, highly suitable solution by transforming the calming process of cooking into a low-pressure, interactive digital experience. Unlike high-stress cooking games, "Star Café" prioritizes a tranquil environment and process-oriented gameplay. Players use intuitive controls in a cozy kitchen, performing simple actions like chopping and frying. Features include ingredient interaction, recipe completion without strict timers, a rewards system, and a save/load system, allowing flexible play. "Star Café" aims to leverage gaming habits, providing familiar, enjoyable stress relief through its cozy atmosphere, soothing audio, and lack of pressure, demonstrating the potential of cozy games for digital well-being.

**Keywords:** Stress Relief, Cozy Game, Cooking Simulation, Game Development, Unity, C#, Digital Well-being

---

## Key Features

* **Relaxing First-Person Gameplay:** Navigate a cozy kitchen environment from a first-person perspective.
* **Interactive Cooking Mechanics:**
    * Pick up, drop, and place ingredients.
    * Utilize different cooking stations 
    * Follow recipes displayed in-game.
* **Order & Delivery System:** Receive orders via an in-game display and deliver completed dishes to a tray.
* **Reward System:**
    * Unlock new recipes and background music tracks by completing dishes.
    * Receive notifications for unlocked rewards.
* **Save/Load System:**
    * Multiple save slots (currently 3).
    * Progress (dishes completed, unlocked recipes, unlocked music) is saved and can be continued.
    * Auto-save on quitting the application or exiting to the main menu from the game.
* **Dynamic Kitchen Environment:**
    * Interactable elements like a radio to change background music.
    * An interactive cat companion that wanders, meows, and reacts to petting with sound and VFX.
    * An interactive recipe book to view recipe images.
    * Lighting and basic post-processing effects to enhance the cozy atmosphere.
* **User Interface:**
    * Main Menu with New Game, Continue, Settings, Credits, and Exit options.
    * In-Game Pause Menu with Resume, Settings, Help, and Exit to Main Menu options.
    * Settings panels with volume controls (Master, Music, SFX via AudioMixer) and a toggle for Bloom post-processing effect.
    * Contextual interaction prompts ([E] and [Q] keys) based on the object the player is looking at and its layer.
* **VFX & SFX:** Particle effects for cooking actions, rewards, and interactions, along with sound effects for a more immersive experience.

---

## Technologies Used

* **Game Engine:** Unity (Version 2022.3.19f1) 
* **Programming Language:** C#
* **Input System:** Unity's New Input System
* **UI:** Unity UI with TextMeshPro
* **Audio:** Unity Audio System, AudioMixer
* **Post-Processing:** Unity's Post-Processing (via URP Volume or Post Processing Stack v2)
* **Data Persistence:** JSON serialization (`JsonUtility`) for save files, `PlayerPrefs` for settings.
* **Version Control (Implied):** Git & GitHub

---

## Setup & Installation (For a Built Game)

1.  Download the latest release ZIP file from https://abeysinghejv.itch.io/star-cafe 
2.  Extract the contents of the ZIP file to a folder on your computer.
3.  Run the `StarCafe.exe` file.


---

## How to Play (Basic Controls)

* **Movement:** WASD keys
* **Look Around:** Mouse
* **Interact (Pickup, Drop, Pet, Use Radio/Book, etc.):** E key
* **Cook/Process (on Stations):** Q key (can be a press or a hold depending on the station)
* **Pause Game:** Escape (Esc) key

*(Refer to the in-game Help menu for more detailed instructions if implemented).*

---

## Future Scope (Potential Enhancements)

* Addition of more recipes, ingredients, and cooking mechanics (e.g., baking).
* Kitchen customization options (colors, furniture, decor) as rewards.
* More advanced interactions with the cat or other environmental elements.
* Expansion to other platforms (e.g., mobile).
* Structured system for collecting and viewing unlocked cat facts.
* More detailed tutorial/onboarding for new players.
* Refinements to UI/UX, including confirmation for overwriting save files.

---

## Development Notes & Known Quirks

* The save system uses `Application.persistentDataPath` to store save files in a "Saves" subfolder.
* Reward milestones and individual awarded status (for pop-ups) are currently managed via a combination of `GameDataManager` (for core progress) and `PlayerPrefs` (for pop-up shown status).
* The project uses specific layer names for interaction detection (e.g., "Pickupable", "IngredientSources", "CatLayer", "Counter1", "Counter2", "Radio", "Book"). These must be set up correctly in the Unity Editor's Tags and Layers settings.

---

Last Updated: May 19, 2025
