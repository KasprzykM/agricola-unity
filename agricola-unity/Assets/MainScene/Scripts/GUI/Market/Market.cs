﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class Market : MonoBehaviour
{
    private GameController gameController;
    private GameObject windowObject;
    private Button buttonFinish;
    private Button buttonAccept;
    private Slider slider;
    public GameObject[] marketItems = new GameObject[numItemSlots];
    public ItemType[] itemTypes = new ItemType[numItemSlots];
    public Image[] itemImages = new Image[numItemSlots];
    public Text[] itemsNames = new Text[numItemSlots];
    public Text[] itemsPrices = new Text[numItemSlots];
    public InputField[] quantities = new InputField[numItemSlots];
    public int totalPrice = 0;
    public static readonly int marketItemWidth = 130;

    public const int numItemSlots = 7;

    public void Initialize()
    {
        int i = 0;
        foreach (ItemType value in ItemType.list)
        {
            //Buy and can be bought or sell and can be sold
            if (slider.value == 0 && value.canBeBought || slider.value == 1 && value.canBeSold)
            {
                marketItems[i].SetActive(true);
                itemTypes[i] = value;
                itemsNames[i].text = value.name;
                //Buy
                if (slider.value == 0)
                    itemsPrices[i].text = "" + value.priceBuy;
                //Sell
                else
                    itemsPrices[i].text = "" + value.priceSell;
                quantities[i].text = "0";
                quantities[i].onValueChanged.AddListener(delegate { ActualizeTotalPrice(); });
                itemImages[i].sprite = Resources.Load<Sprite>(value.directory);
                itemImages[i].enabled = true;
                i++;
            }
        }
        for (int j = i; j < numItemSlots; j++)
            marketItems[j].SetActive(false);
    }

    public void SetMarket()
    {
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        windowObject = GameObject.Find("MarketObject");
        buttonFinish = GameObject.Find("ButtonFinish").GetComponent<Button>();
        buttonAccept = GameObject.Find("ButtonAccept").GetComponent<Button>();
        slider = GameObject.Find("Slider").GetComponent<Slider>();
        slider.onValueChanged.AddListener(delegate { Initialize(); });
        buttonFinish.onClick.AddListener(delegate { Hide(); });
        buttonAccept.onClick.AddListener(delegate { AcceptTransaction(); });
        Initialize();
    }

    public void Display()
    {
        windowObject.SetActive(true);
        ActionController.isActive = false;
    }

    public void Hide()
    {
        windowObject.SetActive(false);
        ActionController.isActive = true;
    }

    public void ActualizeTotalPrice()
    {
        totalPrice = 0;
        for(int i = 0; i<numItemSlots; i++)
        {
            totalPrice += Int32.Parse(quantities[i].text) * Int32.Parse(itemsPrices[i].text);
        }
        GameObject.Find("TotalPrice").GetComponent<Text>().text = totalPrice.ToString();
    }
    public void AcceptTransaction()
    {
        //Buy
        if(slider.value == 0)
            if(gameController.GetMoney() >= totalPrice)
            {
                for (int i = 0; i < numItemSlots; i++)
                {
                    try
                    {
                        try
                        {
                            if (Int32.Parse(quantities[i].text) != 0)
                                gameController.inventory.AddItem(itemTypes[i], Int32.Parse(quantities[i].text));
                        }
                        catch (FormatException e) { }
                    }
                    catch (FormatException e)
                    {
                        gameController.DisplayInfo("Invalid quantity.");
                        break;
                    }
                }
                gameController.MoneyTransaction(-totalPrice);
                for (int i = 0; i < numItemSlots; i++)
                    quantities[i].text = "0";
            }
            else
            {
                gameController.DisplayInfo("You do not have enough money.");
            }
        //Sell
        else
            {
            int isValid = 0;
            for (int i = 0; i < numItemSlots; i++)
            {
                if (Int32.Parse(quantities[i].text) == 0)
                    isValid++;
                else
                    for (int j = 0; j<Inventory.numItemSlots; j++)
                    {
                        if (gameController.inventory.types[j] != null && itemTypes[i].Equals(gameController.inventory.types[j]))
                        {
                            try
                            {
                                if (gameController.inventory.quantities[j].text == null)
                                    ;
                                else if (Int32.Parse(quantities[i].text) > Int32.Parse(gameController.inventory.quantities[j].text))
                                    ;
                                else
                                    isValid++;
                            }
                            catch (FormatException e)
                            {
                                gameController.DisplayInfo("Invalid quantity.");
                                return;
                            }
                        }
                    }
            }
            if(isValid == numItemSlots)
            {
                for (int i = 0; i < numItemSlots; i++)
                {
                    if (Int32.Parse(quantities[i].text) != 0)
                        gameController.inventory.RemoveItem(itemTypes[i], Int32.Parse(quantities[i].text));
                }
                gameController.MoneyTransaction(+totalPrice);
                for (int i = 0; i < numItemSlots; i++)
                    quantities[i].text = "0";
            }
            else
            {
                gameController.DisplayInfo("You do not have enough items.");
            }
        }
    }
}
