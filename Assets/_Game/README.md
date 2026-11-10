# Empire Builder: From Zero to Tycoon

**Game Development Guide Reference:** `Assets/Game Development Documents/Empire_Builder_Full_Game_Development_Guide.pdf`  
**Engine & Pipeline:** Unity 6 (`6000.6.7f1`) with Universal Render Pipeline (URP 17.6) and UGUI (`TMPro`).

---

## 🎮 How to Play / Quick Start

1. Open the project in **Unity 6**.
2. In the top Unity Editor menu bar, click:  
   👉 **`Tools` → `Empire Builder` → `2. Build Playable City Scene & UI`**  
   *(This automatically creates all 3 MVP Asset ScriptableObjects, Game Events, and builds `Assets/_Game/Scenes/City.unity` with camera, slots, and HUD).*
3. Open `Assets/_Game/Scenes/City.unity` and click **Play** (▶️)!

---

## 🕹️ Controls

- **WASD / Arrow Keys:** Pan isometric camera across the city.
- **Left Shift:** Fast camera pan.
- **Mouse Scroll Wheel:** Zoom in / out.
- **Q / E:** Rotate camera angle around the neighborhood.
- **Left Click:** Click any 3D asset plot in the city to open its **Inspection & Financial Detail Panel**.

---

## 🏗️ Architecture & Systems

### 1. Core Data Models (`Assets/_Game/Scripts/Assets/`)
- **`AssetDefinition.cs` (ScriptableObject):** Holds base costs, gross income, operating upkeep, condition decay, and 3D visual prefab.
  - **Taxi:** $5,000 purchase, $120 gross/day, $45 operating/day $\rightarrow$ **+$75/day net**.
  - **Suburban Rental House:** $18,000 purchase, $260 gross/day, $110 operating/day $\rightarrow$ **+$150/day net**.
  - **Vacant Plot:** $8,000 purchase, $0 gross/day, $5 holding cost $\rightarrow$ **-$5/day holding cost**.
- **`OwnedAsset.cs`:** Serializable runtime state (`condition`, `isOperating`, `purchaseDay`, `currentMarketValue`, `state`).

### 2. Economic & Simulation Engine (`Assets/_Game/Scripts/Economy/`, `Simulation/`, `Maintenance/`)
- **`EconomyService.cs`:** Single authority for cash transactions, prevents negative cash, computes Net Worth ($\text{Cash} + \text{Asset Valuations} - \text{Liabilities}$).
- **`SimulationService.cs`:** Advances one day at a time, batches gross revenue and expenses, degrades condition, and compiles the **Daily Financial Report**.
- **`MaintenanceService.cs`:** Dynamic repairs using GDD formula:  
  $$\text{RepairPrice} = \text{BaseRepairCost} \times \left(1 + \frac{100 - \text{Condition}}{100}\right)$$
- **`SaveService.cs`:** JSON persistent save/load system (`Application.persistentDataPath/empire_builder_save.json`).

### 3. UI System (`Assets/_Game/Scripts/UI/`)
- **`TopBarHUD.cs`:** Real-time cash balance, net daily income, total net worth, day counter, and "Advance Day" button.
- **`AssetDetailPanelUI.cs`:** Real-time asset inspector with condition bar, income/cost breakdown, estimated payback period, and action buttons (*Buy*, *Repair*, *Sell*).
- **`DailyReportModalUI.cs`:** Day completion modal showing line-by-line revenue, expenses, repair costs, net cash change, and a pedagogical **Financial Literacy Lesson**.
- **`RandomEventModalUI.cs`:** Dilemma popups (e.g. Engine Warning Light, Roof Leak, Commercial Buyout).

### 4. 3D World Interaction (`Assets/_Game/Scripts/World/`)
- **`WorldAssetSlot.cs`:** Interactive 3D lot that detects player clicks and visually spawns the building/vehicle when purchased.
- **`IsometricCameraController.cs`:** Smooth elevated management camera.

