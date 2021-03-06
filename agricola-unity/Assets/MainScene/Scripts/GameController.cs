﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameController : MonoBehaviour
{
    public List<PlayerController> players;
    public int activePlayer = -1;
    public List<GameObject> timeBarObjects;
    public int maxNumberOfPlayers = 4;
    private Information info;
    public QuestionWindow questionWindow;
    private ItemSelection itemSelection;
    private AnimalFoodWindow animalFoodWindow;
    private Farmland farmland;
    public AnimalFarm animalFarm { get; set; }
    public ActionList actionList;
    public Inventory inventory;
    public DropdownSelect dropdown;
    public int money;
    public readonly int dayLength = 12000;
    private int currentDay;
    private List<ActionController> controlledObjects = new List<ActionController>();
    private System.Random r = new System.Random();
    private string eventsCommunicate = "";
    public readonly int newPlayerCost = 100;
    private int playersAlive = 0;

    private bool isPlayButtonPressed;
    Button playButton;

    public void DoExitGame()
    {
        Application.Quit();
    }

    // Start is called before the first frame update
    void Start()
    {
        farmland = new Farmland();
        animalFarm = new AnimalFarm();
        actionList = new ActionList();

        players = new List<PlayerController>();
        //timeBarObjects = new List<GameObject>();
        //for (int i = 0; i < maxNumberOfPlayers; i++)
            //timeBarObjects.Add(GameObject.Find("TimeBarObject" + i));
        for(int i = 0; i<maxNumberOfPlayers; i++)
            AddNewPlayer();
        players[activePlayer].ActualizeAgeBar();

        isPlayButtonPressed = false;
        playButton = GameObject.Find("PlayButton").GetComponent<Button>();
        inventory = FindObjectOfType<Inventory>();

        info = new Information(GameObject.Find("InformationObject"),
            GameObject.Find("InformationText").GetComponent<Text>());
        info.Hide();

        questionWindow = new QuestionWindow(GameObject.Find("WindowObject"), 
            GameObject.Find("WindowQuestion").GetComponent<Text>(),
            GameObject.Find("ButtonYes").GetComponent<Button>(), 
            GameObject.Find("ButtonNo").GetComponent<Button>());
        questionWindow.Hide();

        ItemType.Initialize();
        itemSelection = FindObjectOfType<ItemSelection>();
        itemSelection.SetMarket();
        itemSelection.Hide();


        MoneyTransaction(0);

        Text dayLabel = GameObject.Find("DayLabel").GetComponent<Text>();
        dayLabel.text = "Day " + (currentDay++).ToString();
        dropdown = FindObjectOfType<DropdownSelect>();
        dropdown.Hide();

        Reset();
    }

    public void Reset()
    {
        animalFarm.reset();
        inventory.Clear();
        farmland.Clear();
        KillPlayers(true);
        MakePlayerActive(10, 30, true);
        SetActivePlayer(0);
        money = 10;
        MoneyTransaction(0);
        inventory.AddItem(ItemType.carrotSeeds, 4);
        currentDay = 0;
        Text dayLabel = GameObject.Find("DayLabel").GetComponent<Text>();
        dayLabel.text = "Day " + (++currentDay).ToString();
        playButton.interactable = true;
        isPlayButtonPressed = false;
        actionList.Clear();
    }

    public void MakePlayerActive(int age, int lifeLength, bool start = false)
    {
        for(int i = 0; i<players.Count; i++)
        {
            if (!players[i].isActive)
            {
                players[i].lifeLength = lifeLength;
                players[i].actionController.age = age;
                players[i].SetActive(start);
                playersAlive++;
                if(activePlayer == -1)
                    SetActivePlayer(i);
                return;
            }
        }
    }

    public void AddNewPlayer()
    {
        players.Add(GameObject.Find("Player" + players.Count).GetComponent<PlayerController>());
        players[players.Count - 1].Setup();
        players[players.Count - 1].SetHomeLocalization(new Vector3(-5.2f , 1.325f, 17f + (players.Count - 1) * 0.8f));
        players[players.Count - 1].SetDeadLocalization(new Vector3(10f + (players.Count - 1), 1.325f, -25f));
    }

    public void AddControlledObject(ActionController actionController)
    {
        controlledObjects.Add(actionController);
    }

    public void RemoveControlledObject(ActionController actionController)
    {
        controlledObjects.Remove(actionController);
    }

    public int GetMoney()
    {
        return money;
    }

    public void MoneyTransaction(int transactionAmount)
    {
        money += transactionAmount;
        GameObject.Find("MoneyValue").GetComponent<Text>().text = money.ToString();
    }

    public int GetMilkSpoilage()
    {
        return animalFarm.getMilkSpoilage();
    }

    public int GetMilkCount()
    {
        return animalFarm.getMilkCount();
    }

    public int GetEggSpoilage()
    {
        return animalFarm.getEggSpoilage();
    }

    public int GetEggCount()
    {
        return animalFarm.getEggCount();
    }

    // once per frame
    void Update()
    {
        for (int i = 0; i < players.Count; i++)
            if (!players[i].isActive)
                players[i].agent.transform.position = players[i].deadPosition;
        if (questionWindow!= null && questionWindow.WasQuestionAsked())
        {
            if (questionWindow.GetQuestionTag() == "Play")
            {
                if (questionWindow.WasQuestionAnswered())
                {
                    if (questionWindow.GetAnswer() == true)
                    {
                        isPlayButtonPressed = true;
                        // Add final action (walk back home) - transparent
                        actionList.Add(null, ActionType.walk);
                        players[activePlayer].ActualizeTimeBar(true);
                    }
                    else
                    {
                        playButton.interactable = true;
                    }
                }
            }
            else if (questionWindow.GetQuestionTag() == "Family member")
            {
                if (questionWindow.WasQuestionAnswered())
                {
                    if (questionWindow.GetAnswer() == true)
                    {
                        if (money >= newPlayerCost)
                        {
                            MoneyTransaction(-newPlayerCost);
                            MakePlayerActive(playersAlive == 2 ? 0 : 10, 20);
                        }
                        else
                        {
                            info.Display("You do not have enough money.");
                        }
                    }
                }
            }
            else if(questionWindow.GetQuestionTag() == "Game over")
            {
                if (questionWindow.WasQuestionAnswered())
                {
                    if (questionWindow.GetAnswer() == true)
                    {
                        Reset();
                    }
                    else
                    {
                        Application.Quit();
                    }
                }
            }
        }

        if (isPlayButtonPressed)
        {
            DoAction();
        }
     
        if(info.hide == true)
        {
            info.Hide();
        }
    }

    // Specifies actions related to the execution of a given action 
    public void PerformAction(Vector3 position) 
    {
        // Custom reaction
        if (actionList.GetAction() == ActionType.walk)
            ;
        else if (actionList.GetAction() == ActionType.plant)
            switch (actionList.GetItemTypeRequired().name)
            {
                case "carrot seeds":
                    farmland.AddPlant(actionList.GetGameObject(), PlantType.carrot);
                    break;
                case "tomato seeds":
                    farmland.AddPlant(actionList.GetGameObject(), PlantType.tomato);
                    break;
                case "pumpkin seeds":
                    farmland.AddPlant(actionList.GetGameObject(), PlantType.pumpkin);
                    break;
            }
        else if (actionList.GetAction() == ActionType.collectPlant)
            farmland.CollectPlant(actionList.GetGameObject());
        else if (actionList.GetAction() == ActionType.placeCow)
            animalFarm.addCow(actionList.GetGameObject());
        else if (actionList.GetAction() == ActionType.gatherMilk)
            animalFarm.gatherMilk();
        else if (actionList.GetAction() == ActionType.market)
        {
            itemSelection.SetMode(ItemSelection.Mode.market);
            itemSelection.Initialize();
            itemSelection.Display();
        }
        else if (actionList.GetAction() == ActionType.eat)
        {
            itemSelection.SetMode(ItemSelection.Mode.eating);
            itemSelection.Initialize();
            itemSelection.Display();
        }
        else if (actionList.GetAction() == ActionType.feedCow)
        {
            //animalFoodWindow.Display();
            itemSelection.SetMode(ItemSelection.Mode.animalEating);
            itemSelection.Initialize();
            itemSelection.animalName = "cows";
            itemSelection.Display();
        }
        else if (actionList.GetAction() == ActionType.placeChicken)
        {
            animalFarm.addChicken(actionList.GetGameObject());
        }
        else if (actionList.GetAction() == ActionType.feedChicken)
        {
            itemSelection.SetMode(ItemSelection.Mode.animalEating);
            itemSelection.Initialize();
            itemSelection.animalName = "chickens";
            itemSelection.Display();
        }
        else if (actionList.GetAction() == ActionType.gatherEgg)
        {
            animalFarm.gatherEgg();
        }
        //Random events
        RandomEvents(actionList.GetAction().associatedEvents);
        // Delete action from queque
        RemoveGameObject(actionList.Remove(0).gameObject);
    }

    public void RandomEvents(IReadOnlyCollection<ActionEvent> eventsCollection)
    {
        foreach (ActionEvent actionEvent in eventsCollection)
        {
            int rInt = r.Next(0, 100);
            // Perform event
            if (100 - actionEvent.probability * 100 <= rInt)
            {
                eventsCommunicate += actionEvent.description + "\n\n";
                PerformEventAction(actionEvent);
                break;
            }
        }
    }

    private void PerformEventAction(ActionEvent actionEvent)
    {
        players[activePlayer].ChangeHalth(actionEvent.healthChange);
        players[activePlayer].ChangeHunger(actionEvent.hungerChange);
        foreach (ItemType itemType in actionEvent.itemsChange.Keys)
        {
            int value = actionEvent.itemsChange[itemType];
            if (value > 0)
                inventory.AddItem(itemType, value);
            else if (value < 0)
                inventory.RemoveItem(itemType, -value);
        }
        
    }

    public void StartActionQueue()
    {
        playButton.interactable = false;
        if(actionList.ActionsLengthsSum() < dayLength)
        {
            double timeUsed = (double)actionList.ActionsLengthsSum() / 1000;
            double timeLeft = ((double)dayLength / 1000 - timeUsed);
            questionWindow.DisplayQuestion("You used " + timeUsed + "h of your day time, " + 
                "but you still have " + timeLeft +  "h. " +
                "Are you sure you want to continue?", "Play");
            isPlayButtonPressed = false;
        }
        else
        {
            isPlayButtonPressed = true;
            if(actionList.GetAction() == ActionType.market)
                actionList.Add(null, ActionType.walk);
            // Add final action (walk back home) - transparent
            actionList.Add(null, ActionType.walk);
        }
    }

    // Manages the execution of the action queue. 
    public void DoAction()
    {
        if (players[activePlayer].IsActionFinished())
        {
            //action finished, check for next
            if (actionList.Count() > 0)
            {
                if (!itemSelection.isVisible || itemSelection.mode == ItemSelection.Mode.market)
                {
                    if (actionList.GetGameObject() == null)
                        if (actionList.Count() == 1 && !itemSelection.isVisible && !questionWindow.isVisible)        // Last action, just walk home
                            players[activePlayer].SetDestination(players[activePlayer].homePosition);
                        else                                // Market action, walk somewhere
                            players[activePlayer].SetDestination(players[activePlayer].deadPosition);

                    else if(!itemSelection.isVisible && !questionWindow.isVisible)
                        players[activePlayer].SetDestination(actionList.GetDestination());

                    if (players[activePlayer].IsPathFinished() && !itemSelection.isVisible && !questionWindow.isVisible)
                    {
                        if (actionList.Count() > 0)
                        {
                            //at the destination, perform actions
                            players[activePlayer].SetAction(actionList.GetAction());
                        }
                    }
                }
            }
            else
            {
                NextDay();
            }
        }
    }

    public void OnClickPlayButton()
    {
        players[activePlayer].lastDayPerformed = currentDay;
        StartActionQueue();
    }

    private void PlayerToListEnd(int id)
    {
        PlayerController playerToMove = players[id];
        //GameObject timeBarObject = timeBarObjects[id];

        for(int i = id; i<maxNumberOfPlayers-1; i++)
        {
            players[i] = players[i+1];
            //players[i].id = players.IndexOf(players[i]);

            //timeBarObjects[i] = timeBarObjects[i + 1];
        }
        players[maxNumberOfPlayers - 1] = playerToMove;
        //players[maxNumberOfPlayers - 1].id = maxNumberOfPlayers - 1;
        //players[maxNumberOfPlayers - 1].id = players.IndexOf(players[maxNumberOfPlayers - 1]);

        //timeBarObjects[maxNumberOfPlayers - 1] = timeBarObject;
    }

    private void KillPlayers(bool forced = false)
    {
        for (int i = 0; i < players.Count; i++) //foreach player
        { 
            if (players[i].isActive)
            {
                if (forced || players[i].GetComponent<ActionController>().age > players[i].lifeLength || !players[i].IsAlive())
                {
                    playersAlive--;
                    players[i].SetInactive();
                    PlayerToListEnd(i);
                    i--;
                    if (!forced)
                    {
                        eventsCommunicate += "One of your subordinates died." + "\n";
                        questionWindow.DisplayQuestion(eventsCommunicate, "Action event", true);
                        eventsCommunicate = "";
                    }
                    if (!forced && playersAlive == 0) //we do not have more players
                    {
                        //todo save Score to file?
                        int inventoryValue = inventory.GetInventoryValue() + money;
                        questionWindow.DisplayQuestion("All yours subordinates died. The game is over. Do you want to play again? \nScore: " + inventoryValue +
                            "\nDay: " + (currentDay - 1), "Game over");
                        activePlayer = -1;
                    }
                    else if (!forced)
                    {
                        // Set to first ACTIVE
                        for (int j = 0; j < maxNumberOfPlayers; j++)
                            if (players[j].isActive)
                            {
                                SetActivePlayer(j);
                            }
                    }
                    else
                    {
                        SetActivePlayer(0);
                    }
                }
            }
        }
    }

    public void SetActivePlayer(int i)
    {
        activePlayer = i;
        players[activePlayer].ActualizeHealthBar();
        players[activePlayer].ActualizeHungerBar();
        players[activePlayer].ActualizeAgeBar();
        players[activePlayer].ActualizeIcon();
    }

    public void PerformMouseExit()
    {
        foreach (ActionController actionController in controlledObjects)
            actionController.OnMouseExit();
    }

    // Applies some changes to the game view
    public void NextDay()
    {
        isPlayButtonPressed = false;
        playButton.interactable = true;
        if (eventsCommunicate != "")
        {
            questionWindow.DisplayQuestion(eventsCommunicate, "Action event", true);
            eventsCommunicate = "";
        }
        // If current player performed this day, SET to NEXT active
        if(players[activePlayer].lastDayPerformed == currentDay)
            for(int i = 0; i < maxNumberOfPlayers; i++)
            {
                if (players[i].isActive && players[i].lastDayPerformed != currentDay)
                {
                    SetActivePlayer(i);
                    return;
                }
            }
        // All players performed this day
        
        SetActivePlayer(0);

        farmland.GrowPlants();
        for(int i = 0; i<players.Count; i++)
            if(players[i].isActive)
                players[i].ActualizeTimeBar();
        Text dayLabel = GameObject.Find("DayLabel").GetComponent<Text>();
        dayLabel.text = "Day " + (++currentDay).ToString();
        animalFarm.spoilFood();
        animalFarm.generateFoodProducts();
        animalFarm.ageAnimals();
        animalFarm.animalsEat();
        animalFarm.clearRottenOnlyFood();
        foreach (ActionController actionController in controlledObjects)
            actionController.age += 1;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].isActive)
            {
                if (!players[i].IsHungry())
                    players[i].ChangeHalth(+5);
                if(players[i].IsStarving())
                    players[i].ChangeHalth(-20);
                players[i].ChangeHunger(-25);
                players[i].ActualizeAgeBar();
            }
        }

        KillPlayers();
        if (eventsCommunicate != "")
        {
            questionWindow.DisplayQuestion(eventsCommunicate, "Action event", true);
            eventsCommunicate = "";
        }
    }

    /* Action is not allowed: 
     * 1) if there the same action in queque (same type, same object)
     * 2) planting area is already taken
     * 3) plant can not be collected yet (baby or spoiled plant)
     */
    public string IsAcctionAllowed(GameObject gameObject, ActionType type)
    {
        if (isPlayButtonPressed)
            return "Animation is in progress.";
        if (actionList.IsActionInQueque(gameObject, type)) // TODO: blad tutaj jest z wyswietlaniem komunikatu kiedy action przekroczy sie limit czasu uzywajac tylko krów.
            return "Action already in queue.";
        if (actionList.ActionsLengthsSum() + type.length > dayLength)
            return "Action too long. " + ((double)(dayLength - actionList.ActionsLengthsSum()) / 1000) + "h left.";
        if (type == ActionType.plant)
        {
            if (farmland.IsAreaTaken(gameObject))
                return "This area is already taken.";
            if (!inventory.DoesContain(dropdown.GetSelected()))
                return "You do not have relevant seeds.";
        }
        if (type == ActionType.collectPlant)
            if (!farmland.CanPlantBeCollected(gameObject))
                return "Plant can not be collected.";
        if (type == ActionType.placeCow)
            if (!animalFarm.isSlotAvailable(gameObject))
                return "Slots taken by another cow.";
            else if (!inventory.DoesContain(ItemType.cow))
                return "You don't have any cows.";
        if (type == ActionType.gatherMilk)
            if (animalFarm.getMilkCount() <= 0)
                return "No milks to gather.";
        if (type == ActionType.checkCowStatus || type == ActionType.checkChickenStatus)
            return "No action to be done.";
        if (type == ActionType.placeChicken)
            if (!animalFarm.isSlotAvailable(gameObject))
                return "Slots taken by another chicken.";
            else if (!inventory.DoesContain(ItemType.chicken))
                return "You don't have any chickens.";
        if (type == ActionType.gatherEgg)
            if (animalFarm.getEggCount() <= 0)
                return "No eggs to gather.";
        return null;
    }

    // Add action only if animation is NOT in progress
    public void AddAction(GameObject gameObject, ActionType type)
    {
        string message = IsAcctionAllowed(gameObject, type);
        if (message == null)
        {
            if (type == ActionType.plant)
            {
                switch (dropdown.GetSelected())
                {
                    case "carrot seeds":
                        actionList.Add(gameObject, type, ItemType.carrotSeeds);
                        break;
                    case "tomato seeds":
                        actionList.Add(gameObject, type, ItemType.tomatoSeeds);
                        break;
                    case "pumpkin seeds":
                        actionList.Add(gameObject, type, ItemType.pumpkinSeeds);
                        break;
                }
            }
            else if (type == ActionType.placeCow)
                actionList.Add(gameObject, type, ItemType.cow);
            else if (type == ActionType.placeChicken)
                actionList.Add(gameObject, type, ItemType.chicken);
            else
                actionList.Add(gameObject, type);
        }

            
        else
            info.Display("Not allowed. " + message);
 
    }

    public void DisplayInfo(string message)
    {
        info.Display(message);
    }

    // Providing Instantiate method for other classes (context problem)
    public GameObject InstantiatePrefab(UnityEngine.Object prefab, Vector3 vector, Quaternion identity)
    {
        return Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
    }
    // Providing Destroy method for other classes (context problem)
    public static void RemoveGameObject(GameObject gameObject)
    {
        Destroy(gameObject);
    }
}
