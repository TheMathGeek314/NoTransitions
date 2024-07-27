using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Modding;
using GlobalEnums;

namespace NoTransitions {
    public class NoTransitions: Mod {
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0060 // Remove unused parameter
        new public string GetName() => "NoTransitions";
        public override string GetVersion() => "v0.9.3.0";
        public string currentScene = "";
        public bool shouldDefaultLoad = true;
        public static int transitionsTouchingCounter;
        public List<string> transitionsTouching = new();
        public List<string> roomsBeingLoaded = new();
        private static Collider2D knightCollider;
        private GameObject hazardBox;

        public override void Initialize() {
            On.GameManager.OnNextLevelReady += initialRoomLoad;
            CameraMods.Initialize();
            On.TransitionPoint.Awake += add2DProxy;
            On.TransitionPoint.OnTriggerEnter2D += doNothing;
            //On.TransitionPoint.OnTriggerStay2D += doNothing2;
            On.PlayMakerUnity2DProxy.OnTriggerEnter2D += transitionEnterHook;
            On.PlayMakerUnity2DProxy.OnTriggerExit2D += transitionExitHook;

            //ModHooks.HeroUpdateHook += KeyTest;
            //USceneManager.activeSceneChanged += printActiveSceneChanged;
            On.GameManager.LevelActivated += levelActivatedTest;
        }

        private void levelActivatedTest(On.GameManager.orig_LevelActivated orig, GameManager self, Scene sceneFrom, Scene sceneTo) {
            //Log("levelActivated(" + sceneFrom.name + ", " + sceneTo.name + ") - currentScene=" + currentScene);
            if(isNonGameplayScene(sceneTo.name) || String.IsNullOrEmpty(sceneFrom.name)) {
                orig(self,sceneFrom,sceneTo);
            }
        }

        /*private void printActiveSceneChanged(Scene arg0, Scene arg1) {
            Log("activeSceneChanged(" + arg0.name + ", " + arg1.name + "), currentScene = " + currentScene);
        }*/

        /*private void KeyTest() {
            if(Input.GetKeyDown(KeyCode.Y)) {
                printAllObjects();
            }
        }*/

        private void initialRoomLoad(On.GameManager.orig_OnNextLevelReady orig, GameManager self) {
            Log("Called initialRoomLoad with shouldDefaultLoad = " + shouldDefaultLoad);
            if(shouldDefaultLoad || true) {
                //Log("MathGeek stack trace: " + StackTraceUtility.ExtractStackTrace());
                //if(string.IsNullOrEmpty(currentScene)) {
                currentScene = self.sceneName;
                //}
                //Log("customTransitionLateing with sceneName = " + currentScene);
                orig(self);
                transitionsTouchingCounter = 0;
                //string sceneName = self.sceneName;
                ///*testing*/sceneName = currentScene;
                prepScene(currentScene);
                unloadOldRooms(currentScene);
                //currentScene = sceneName;
                //GameManager.instance.sceneName = sceneName;
                //USceneManager.SetActiveScene(USceneManager.GetSceneByName(sceneName));
                loadNewRooms(currentScene);
            }
            shouldDefaultLoad = true;
        }

        private void transitionEnterHook(On.PlayMakerUnity2DProxy.orig_OnTriggerEnter2D orig, PlayMakerUnity2DProxy self, Collider2D movingObj) {
            TransitionPoint tp = self.gameObject.GetComponent<TransitionPoint>();
            if(tp != null) {
                string trackerName = self.gameObject.scene.name + ":" + self.gameObject.name;
                if(!transitionsTouching.Contains(trackerName)) {
                    transitionsTouching.Add(trackerName);
                    if(isValidTransition(tp, movingObj)) {
                        //Log("Entered " + trackerName);
                        transitionsTouchingCounter++;
                        transitionEnter(tp);
                    }
                }
            }
            else {
                orig(self, movingObj);
            }
        }

        private void transitionExitHook(On.PlayMakerUnity2DProxy.orig_OnTriggerExit2D orig, PlayMakerUnity2DProxy self, Collider2D coll) {
            TransitionPoint tp = self.gameObject.GetComponent<TransitionPoint>();
            if(tp != null) {
                string trackerName = self.gameObject.scene.name + ":" + self.gameObject.name;
                if(transitionsTouching.Contains(trackerName)) {
                    transitionsTouching.Remove(trackerName);
                    if(isValidTransition(tp, coll)) {
                        //Log("Exited " + trackerName);
                        transitionsTouchingCounter--;
                        transitionExit(tp);
                    }
                }
            }
            else {
                orig(self, coll);
            }
        }

        private void transitionEnter(TransitionPoint self) {
            if(self.enabled) {
                self.enabled = false;
                GameObject[] gObjs = USceneManager.GetSceneByName(self.targetScene).GetRootGameObjects();
                foreach(GameObject go in gObjs) {
                    if(go.GetComponent<TransitionPoint>() == null) {
                        Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>();
                        foreach(Collider2D coll in colliders) {
                            Physics2D.IgnoreCollision(knightCollider, coll, false);
                        }
                    }
                }
                List<TransitionPoint> futureRoomExits = getTransitions(self.targetScene);
                foreach(TransitionPoint exit in futureRoomExits) {
                    if(exit.targetScene == self.gameObject.scene.name) {
                        exit.enabled = false;
                    }
                    else {
                        loadSingleAdjacentRoom(exit);
                    }
                    exit.gameObject.SetActive(true);
                }
            }
        }

        private void transitionExit(TransitionPoint self) {
            if(transitionsTouchingCounter == 0) {
                string oldRoom = self.gameObject.scene.name;
                string newRoom = currentScene = GameManager.instance.sceneName = self.targetScene;
                USceneManager.SetActiveScene(USceneManager.GetSceneByName(newRoom));
                GameObject[] gObjs = USceneManager.GetSceneByName(oldRoom).GetRootGameObjects();
                foreach(GameObject go in gObjs) {
                    if(go.GetComponent<TransitionPoint>() == null) {
                        Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>();
                        foreach(Collider2D coll in colliders) {
                            Physics2D.IgnoreCollision(knightCollider, coll);
                        }
                    }
                }
                //re-enable the current transition behind you
                List<TransitionPoint> newRoomExits = getTransitions(newRoom);
                foreach(TransitionPoint currentExit in newRoomExits) {
                    if(currentExit.targetScene == oldRoom) {
                        currentExit.enabled = true;
                        break;
                    }
                }
                //disable distant transition objects and unload target rooms
                shouldDefaultLoad = false;
                List<TransitionPoint> oldRoomExits = getTransitions(oldRoom);
                foreach(TransitionPoint oldExit in oldRoomExits) {
                    string adjacentRoom = oldExit.targetScene;
                    oldExit.gameObject.SetActive(false);
                    if(adjacentRoom != newRoom) {
                        if(USceneManager.GetSceneByName(adjacentRoom).IsValid()) {
                            //Log("Unloading the " + adjacentRoom);
                            USceneManager.UnloadSceneAsync(adjacentRoom);
                        }
                    }
                }
                if(hazardBox == null) {
                    hazardBox = GameObject.Find("Bounds Cage");
                }
                Vector3 knightPosition = HeroController.instance.gameObject.transform.position;
                hazardBox.transform.position = new Vector3(knightPosition.x, knightPosition.y + 200, hazardBox.transform.position.z);
                defaultTransitionStuff(self);
                //Log("Transitioned to " + newRoom);
            }
        }

        private void unloadOldRooms(string enteringScene) {
            List<string> toUnload = new();
            for(int i = 0; i < USceneManager.sceneCount; i++) {
                string scene = USceneManager.GetSceneAt(i).name;
                if(scene != enteringScene && scene != "DontDestroyOnLoad" && scene != "HideAndDontSave") {
                    toUnload.Add(scene);
                }
            }
            foreach(string oldScene in toUnload) {
                Log("Unloading " + oldScene);
                USceneManager.UnloadSceneAsync(oldScene);
            }
        }

        private void loadNewRooms(string activeScene) {
            List<TransitionPoint> currentExits = getTransitions(activeScene);
            foreach(TransitionPoint currentExit in currentExits) {
                loadSingleAdjacentRoom(currentExit);
                currentExit.gameObject.SetActive(true);
            }
        }

        private async void loadSingleAdjacentRoom(TransitionPoint transitionFromFutureRoom) {
            bool needToLoad;
            AsyncOperation loadOp = new();
            string adjacentRoom = transitionFromFutureRoom.targetScene;
            if(USceneManager.GetSceneByName(adjacentRoom).IsValid() || roomsBeingLoaded.Contains(adjacentRoom)) {
                //room already exists
                needToLoad = false;
            }
            else {
                roomsBeingLoaded.Add(adjacentRoom);
                loadOp = USceneManager.LoadSceneAsync(adjacentRoom, LoadSceneMode.Additive);
                needToLoad = true;
            }
            while(needToLoad && !loadOp.isDone) {
                await Task.Yield();
            }
            roomsBeingLoaded.Remove(adjacentRoom);
            prepScene(adjacentRoom);
            //find the returning transition and use it to move everything in the scene over
            List<TransitionPoint> transitionsFromAdjacentRoom = getTransitions(adjacentRoom);
            Vector2 currentCorner = getCorner(transitionFromFutureRoom.gameObject);
            if(knightCollider==null)
                knightCollider = HeroController.instance.gameObject.GetComponentInChildren<BoxCollider2D>();
            foreach(TransitionPoint adjacentExit in transitionsFromAdjacentRoom) {
                if(adjacentExit.name == transitionFromFutureRoom.entryPoint) {
                    Vector2 diff = currentCorner - getCorner(adjacentExit.gameObject);
                    GameObject[] adjacentRoomObjects = USceneManager.GetSceneByName(adjacentRoom).GetRootGameObjects();
                    foreach(GameObject go in adjacentRoomObjects) {
                        bool isEnabled = go.activeSelf;
                        go.SetActive(false);
                        Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>();
                        foreach(Collider2D coll in colliders) {
                            if(coll.gameObject.GetComponent<TransitionPoint>() == null) {
                                Physics2D.IgnoreCollision(knightCollider, coll);
                            }
                        }
                        go.transform.position = new Vector3(go.transform.position.x + diff.x, go.transform.position.y + diff.y, go.transform.position.z);
                        go.SetActive(isEnabled);
                        postLoadReactivation(adjacentRoom);
                    }
                }
                adjacentExit.gameObject.SetActive(false);
            }
        }

        private void add2DProxy(On.TransitionPoint.orig_Awake orig, TransitionPoint self) {
            orig(self);
            PlayMakerUnity2DProxy pm = self.gameObject.AddComponent<PlayMakerUnity2DProxy>();
            pm.HandleTriggerExit2D = true;
            pm.HandleTriggerEnter2D = true;
        }

        private async void prepScene(string sceneName) {
            Log("Prepping scene " + sceneName);
            Scene currentRoomScene = USceneManager.GetSceneByName(sceneName);
            int attempts = 0;
            while(!currentRoomScene.IsValid()) {
                attempts++;
                if(attempts == 100) {
                    Log("Could not get valid scene " + sceneName);
                    return;
                }
                await Task.Yield();
            }
            GameObject[] gObjs = USceneManager.GetSceneByName(sceneName).GetRootGameObjects();
            foreach(GameObject obj in gObjs) {
                List<GameObject> objChildren = new(obj.GetComponentsInChildren<GameObject>(true));
                objChildren.Add(obj);
                for(int i = 0; i < objChildren.Count; i++) {
                    GameObject objChild = objChildren.ElementAt(i);
                    if(objChild.name == "SceneBorder(Clone)") {
                        objChild.SetActive(false);
                    }
                    else if(objChild.name.StartsWith("black_solid")) {
                        objChild.SetActive(false);
                    }
                    else if(sceneName == "Crossroads_01" && objChild.name == "_Transition gates") {
                        TransitionPoint[] tps = objChild.GetComponentsInChildren<TransitionPoint>();
                        foreach(TransitionPoint tp in tps) {
                            if(tp.name == "top1") {
                                tp.entryPoint = "";
                            }
                            else if(tp.name == "top2") {
                                tp.entryPoint = "bot1";
                            }
                        }
                    }
                    else if(sceneName == "Crossroads_21" && objChild.name == "_Transition Gates") {
                        TransitionPoint[] tps = objChild.GetComponentsInChildren<TransitionPoint>();
                        foreach(TransitionPoint tp in tps) {
                            if(tp.name == "top2" || tp.gameObject.name == "top2") {
                                GameObject.Destroy(tp);
                            }
                        }
                    }
                    else if(sceneName == "Crossroads_49b" && objChild.name == "left1") {
                        GameObject.Destroy(objChild);
                    }
                    else if(sceneName == "Town" && objChild.name == "bot1") {
                        objChild.GetComponent<TransitionPoint>().entryPoint = "top2";
                    }
                }
            }
        }

        private async void postLoadReactivation(string sceneName) {
            do {
                await Task.Yield();
            } while(false);
            GameObject[] gameObjects = USceneManager.GetSceneByName(sceneName).GetRootGameObjects();
            foreach(GameObject go in gameObjects) {
                Climber[] climbers = go.GetComponentsInChildren<Climber>();
                if(climbers.Length > 0) {
                    foreach(Climber climber in climbers) {
                        typeof(Climber).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(climber, null);
                    }
                }
            }
        }

        private List<TransitionPoint> getTransitions(string sceneName) {
            return getTransitions(sceneName, false);
        }

        private List<TransitionPoint> getTransitions(string sceneName, bool shouldLog) {
            List<TransitionPoint> transitions = new();
            if(!USceneManager.GetSceneByName(sceneName).IsValid()) {
                Log("Could not get transitions from invalid scene " + sceneName);
                return transitions;
            }
            GameObject[] gos = USceneManager.GetSceneByName(sceneName).GetRootGameObjects();
            foreach(GameObject go in gos) {
                TransitionPoint[] tps = go.GetComponentsInChildren<TransitionPoint>(true);
                foreach(TransitionPoint tp in tps) {
                    if(tp == null) {
                        continue;
                    }
                    if(string.IsNullOrEmpty(tp.targetScene) || string.IsNullOrEmpty(tp.entryPoint)) {
                        continue;
                    }
                    if(tp.isADoor) {
                        continue;
                    }
                    transitions.Add(tp);
                    if(shouldLog)
                        Log("\t\tFound transition " + tp.name + " to " + tp.targetScene);
                }
            }
            return transitions;
        }

        private void defaultTransitionStuff(TransitionPoint self) {
            if(self.atmosSnapshot != null)
                self.atmosSnapshot.TransitionTo(1.5f);
            if(self.enviroSnapshot != null)
                self.enviroSnapshot.TransitionTo(1.5f);
            if(self.actorSnapshot != null)
                self.actorSnapshot.TransitionTo(1.5f);
            if(self.musicSnapshot != null)
                self.musicSnapshot.TransitionTo(1.5f);
            TransitionPoint.lastEntered = self.gameObject.name;
        }

        private bool isValidTransition(TransitionPoint tp, Collider2D movingObj) {
            if(!tp.isADoor && movingObj.gameObject.layer == 9 && GameManager.instance.gameState == GameState.PLAYING) {
                if(!string.IsNullOrEmpty(tp.targetScene) && !string.IsNullOrEmpty(tp.entryPoint)) {
                    return true;
                }
            }
            return false;
        }

        private bool isNonGameplayScene(string sceneName) {
            return new List<string> { "Intro_Cutscene_Prologue", "Opening_Sequence", "Prologue_Excerpt", "Intro_Cutscene", "Cinematic_Stag_travel", "PermaDeath",
                "Cinematic_Ending_A", "Cinematic_Ending_B", "Cinematic_Ending_C", "Cinematic_Ending_D", "Cinematic_Ending_E", "Cinematic_MrMushroom", "BetaEnd",
                "Knight Pickup", "Pre_Menu_Intro", "Menu_Title", "End_Credits", "Menu_Credits", "Cutscene_Boss_Door", "PermaDeath_Unlock", "GG_Unlock", "GG_End_Sequence",
                "End_Game_Completion", "BetaEnd", "PermaDeath", "GG_Entrance_Cutscene", "GG_Boss_Door_Entrance" }.Contains(sceneName) || InGameCutsceneInfo.IsInCutscene;
        }

        private void doNothing(On.TransitionPoint.orig_OnTriggerEnter2D orig, TransitionPoint self, Collider2D coll) {}

        private void doNothing2(On.TransitionPoint.orig_OnTriggerStay2D orig, TransitionPoint self, Collider2D movingObj) {}

        private enum Corner {
            TopLeft, TopRight, BottomLeft, BottomRight
        }

        private static Vector2 getCorner(GameObject gameObject) {
            Vector2 extents = gameObject.GetComponent<BoxCollider2D>().bounds.extents;
            Vector2 cornerPositions = gameObject.transform.position;
            string direction = gameObject.name.Substring(0, gameObject.name.Length - 1);
            Corner corner;
            switch(direction) {
                case "left":
                    corner = Corner.BottomRight;
                    break;
                case "bot":
                    corner = Corner.TopLeft;
                    break;
                case "right":
                case "top":
                default:
                    corner = Corner.BottomLeft;
                    break;
            }
            //going right: bottom left
            //going left: bottom right
            //going down: top left
            //going up: bottom left
            if(corner == Corner.TopLeft) {
                cornerPositions.x -= extents.x;
                cornerPositions.y += extents.y;
            }
            else if(corner == Corner.TopRight) {
                cornerPositions += extents;
            }
            else if(corner == Corner.BottomLeft) {
                cornerPositions -= extents;
            }
            else if(corner == Corner.BottomRight) {
                cornerPositions.x += extents.x;
                cornerPositions.y -= extents.y;
            }
            return cornerPositions;
        }

        private void printAllObjects() {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            Log("PRINTING ALL GAMEOBJECTS AND THEIR SCENES");
            Dictionary<string, List<string>> gameObjects = new();
            foreach(GameObject obj in allObjects) {
                string sceneName = obj.scene.name;
                if(!gameObjects.ContainsKey(sceneName)) {
                    gameObjects.Add(sceneName, new List<string>());
                }
                gameObjects[sceneName].Add(obj.name);
            }
            foreach(string key in gameObjects.Keys) {
                gameObjects[key].Sort();
                Log("\t\t" + key);
                foreach(string go in gameObjects[key]) {
                    Log("\t\t\t " + key + ": " + go);
                }
            }
        }
    }
}
