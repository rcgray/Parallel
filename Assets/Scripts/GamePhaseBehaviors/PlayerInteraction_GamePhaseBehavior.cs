﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class PlayerInteraction_GamePhaseBehavior : GamePhaseBehavior {
    public enum InteractionPhases { ingame_default, ingame_dragging, ingame_connecting, simulation, awaitingSimulation }
    public InteractionPhases interactionPhase = InteractionPhases.simulation;

    public enum InGamePhases { none, optionClicked, movingObject, placingObject }
    public enum MenuOptions { pause, semaphore, button, trash, simulate }

    bool flowVisibility = false;
    public bool trashHover = false;
    bool connectVisibility;
    public bool connectVisibilityLock = false;
    public bool colorFlowVisibilityLock = false;

    public PlayerInteraction_UI playerInteraction_UI;

    GridObjectBehavior currentGridObject;

    public LevelScore score;

    bool paused;
    public bool tutorialMode;

    float simulationTime = 0f;
    float stationaryTime = 0f;
    Dictionary<int, List<StepData>> simulationDStepDictionary = new Dictionary<int, List<StepData>>();

    Vector3 stationaryMousePosition;
    GridObjectBehavior hoverObject;
    GridObjectBehavior hoverObject_;

    public LayerMask defaultMask, draggingMask;

    //for zooming
    float originalOrthographicSize = 0f;
    float currentOrthographicSize = 0f;
    float zoomLevel = 0f;
    float maxZoomLevel = 2f;
    Vector3 originalCameraPosition = Vector3.zero;
    Vector3 currentCameraPosition = Vector3.zero;
    bool isZooming = false;

    public delegate void OnSimulationStepDelegate(StepData step);
    public static OnSimulationStepDelegate onSimulationStep;

    public delegate void PauseSimulationDelegate();
    public static PauseSimulationDelegate pauseSimulation;

    public delegate void UnpauseSimulationDelegate();
    public static UnpauseSimulationDelegate unpauseSimulation;

    public delegate void DelayedUnpauseDelegate(float delay, TutorialEvent t);
    public static DelayedUnpauseDelegate delayedUnpause;

    public delegate void OnCompletionDelegate();
    public static OnCompletionDelegate onCompletion;

    public delegate void OnMenuInteractionDelegate(MenuOptions inputMenuOption);
    public static OnMenuInteractionDelegate onMenuInteraction;

    public override void BeginPhase()
    {
        Debug.Log("BeginPhase");
        pauseSimulation += PauseSimulation;
        unpauseSimulation += UnpauseSimulation;
        delayedUnpause += DelayedUnpause;

        playerInteraction_UI.SetText(GameManager.Instance.GetDataManager().currentLevelData);
        playerInteraction_UI.OpenUI();
        DefineButtonBehaviors();

        score.index = GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id;

        GameManager.Instance.TriggerLevelTutorial
        (
            GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id,
            interactionPhase == InteractionPhases.awaitingSimulation || interactionPhase == InteractionPhases.simulation ? TutorialEvent.TutorialInitializeTriggers.duringSimulation : TutorialEvent.TutorialInitializeTriggers.beforePlay
        );

        GameManager.Instance.SetUpLevelInventory();

        //for zooming
        originalOrthographicSize = GameManager.Instance.GetGridManager().worldCamera.orthographicSize;
        currentOrthographicSize = originalOrthographicSize;
        zoomLevel = 0f;
        originalCameraPosition = GameManager.Instance.GetGridManager().worldCamera.transform.position;
        currentCameraPosition = originalCameraPosition;
        isZooming = false;

        if (PlayerPrefs.HasKey("LinkHover"))
        {

        }
        else
        {

        }
        if (PlayerPrefs.HasKey(""))
        {

        }
        else
        {

        }
    }

	public override void UpdatePhase()
	{
		if(interactionPhase == InteractionPhases.simulation)
		{
			GameManager.Instance.TriggerTrackUpdate();
		}
		else 
		{
			PlayerInteractionListener();
		}
	}

	public override void EndPhase()
	{
		playerInteraction_UI.CloseUI();
        LockFlowVisibility(-1);
    }


	public void DefineButtonBehaviors()
	{
		playerInteraction_UI.ClearButtonBehaviors();
		foreach(Transform t in playerInteraction_UI.hint_button_container) { Destroy(t.gameObject); }

		/* semaphore placement events */
			EventTrigger.Entry beginDrag_semaphore = new EventTrigger.Entry();
			beginDrag_semaphore.eventID = EventTriggerType.BeginDrag;
			beginDrag_semaphore.callback.AddListener( (eventData) => { BeginDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(beginDrag_semaphore);

			EventTrigger.Entry continueDrag_semaphore = new EventTrigger.Entry();
			continueDrag_semaphore.eventID = EventTriggerType.Drag;
			continueDrag_semaphore.callback.AddListener( (eventData) => { ContinueDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(continueDrag_semaphore);

			EventTrigger.Entry endDrag_semaphore = new EventTrigger.Entry();
			endDrag_semaphore.eventID = EventTriggerType.EndDrag;
			endDrag_semaphore.callback.AddListener( (eventData) => { EndDrag(MenuOptions.semaphore); } );
			playerInteraction_UI.place_semaphore.triggers.Add(endDrag_semaphore);

		/* signal placement events */
			EventTrigger.Entry beginDrag_button = new EventTrigger.Entry();
			beginDrag_button.eventID = EventTriggerType.BeginDrag;
			beginDrag_button.callback.AddListener( (eventData) => { BeginDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(beginDrag_button);
				
			EventTrigger.Entry continueDrag_button = new EventTrigger.Entry();
			continueDrag_button.eventID = EventTriggerType.Drag;
			continueDrag_button.callback.AddListener( (eventData) => { ContinueDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(continueDrag_button);

			EventTrigger.Entry endDrag_button = new EventTrigger.Entry();
			endDrag_button.eventID = EventTriggerType.EndDrag;
			endDrag_button.callback.AddListener( (eventData) => { EndDrag(MenuOptions.button); } );
			playerInteraction_UI.place_button.triggers.Add(endDrag_button);

		/* trash events */
			EventTrigger.Entry hover_trash = new EventTrigger.Entry();
			hover_trash.eventID = EventTriggerType.PointerEnter;
			hover_trash.callback.AddListener( (eventData) => { BeginHover(MenuOptions.trash); } );
			playerInteraction_UI.trash.triggers.Add(hover_trash);

			EventTrigger.Entry endHover_trash = new EventTrigger.Entry();
			endHover_trash.eventID = EventTriggerType.PointerExit;
			endHover_trash.callback.AddListener( (eventData) => { EndHover(MenuOptions.trash); } );
			playerInteraction_UI.trash.triggers.Add(endHover_trash);
		
        /* Bezier Visibility */
			/*Button flowButton = playerInteraction_UI.preview.GetComponent<Button>();
			flowButton.onClick.RemoveAllListeners();
			flowButton.onClick.AddListener( ()=> ToggleConnectionVisibility() );
            */
            EventTrigger.Entry hover_bezier = new EventTrigger.Entry();
            hover_bezier.eventID = EventTriggerType.PointerEnter;
            hover_bezier.callback.AddListener((eventData) => { connectVisibility = false; ToggleConnectionVisibility(); });
            playerInteraction_UI.preview.triggers.Add(hover_bezier);

			EventTrigger.Entry click_bezier = new EventTrigger.Entry();
			click_bezier.eventID = EventTriggerType.PointerDown;
			click_bezier.callback.AddListener((eventData) => { LockConnectionVisibility(); });
			playerInteraction_UI.preview.triggers.Add(click_bezier);

            EventTrigger.Entry endHover_bezier = new EventTrigger.Entry();
            endHover_trash.eventID = EventTriggerType.PointerExit;
            endHover_trash.callback.AddListener((eventData) => { connectVisibility = true; ToggleConnectionVisibility(); });
            playerInteraction_UI.preview.triggers.Add(endHover_trash);

        /* Exit */
            //Note: Exit Button behavior is handled in playerInteraction_UI.pauseOverlay


		LinkJava.SimulationTypes playSimulation = LinkJava.SimulationTypes.Play;
		playerInteraction_UI.simulationButton.onClick.RemoveAllListeners();
        playerInteraction_UI.simulationButton.onClick.AddListener(() => TriggerSimulation(playSimulation)/*GameManager.Instance.SubmitCurrentLevel(playSimulation)*/ );
        playerInteraction_UI.simulationButton.onClick.AddListener( ()=> playerInteraction_UI.simulationButton.interactable = false );
		playerInteraction_UI.simulationButton.interactable = true;

		playerInteraction_UI.stopSimulationButton.onClick.RemoveAllListeners();
		playerInteraction_UI.stopSimulationButton.onClick.AddListener( ()=> { EndSimulation(); Debug.Log("End Simulation Button hit."); });
		playerInteraction_UI.stopSimulationButton.interactable = false;
		playerInteraction_UI.stopSimulationButton.gameObject.SetActive(false);

		LinkJava.SimulationTypes fullSimulation = LinkJava.SimulationTypes.ME;
		playerInteraction_UI.submitButton.onClick.RemoveAllListeners();
		playerInteraction_UI.submitButton.onClick.AddListener( ()=> TriggerSimulation(fullSimulation)/*GameManager.Instance.SubmitCurrentLevel(fullSimulation)*/ );
		playerInteraction_UI.submitButton.onClick.AddListener( ()=> playerInteraction_UI.submitButton.interactable = false );
        playerInteraction_UI.submitButton.onClick.AddListener(() => score.attemptCount++ );
        playerInteraction_UI.submitButton.interactable = true;

		playerInteraction_UI.revealHintsButton.onClick.RemoveAllListeners();
		playerInteraction_UI.revealHintsButton.onClick.AddListener( ()=> ToggleHintsVisibility() );

/* Track Color Hover Setup */
		for(int triggerIndex = 0; triggerIndex < playerInteraction_UI.rightPanelColors.Length; triggerIndex++)
		{
            playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Clear();
            if ( GameManager.Instance.GetGridManager().IsCurrentThreadColor( triggerIndex ) )
			{
				playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.SetActive(true);
				int loadColors = triggerIndex;// + 1;

                EventTrigger.Entry beginHoverColor = new EventTrigger.Entry();
				beginHoverColor.eventID = EventTriggerType.PointerEnter;
				beginHoverColor.callback.AddListener( (eventData) => {
					if ( !/*connectVisibilityLock*/colorFlowVisibilityLock )  GameManager.Instance.GetGridManager().RevealGridColorMask(loadColors);
                    ToggleFlowVisibility(true);
                } );
				playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(beginHoverColor);

                EventTrigger.Entry lockHoverColor = new EventTrigger.Entry();
                lockHoverColor.eventID = EventTriggerType.PointerDown;
                int lockIndex = triggerIndex;
                lockHoverColor.callback.AddListener((eventData) => { LockFlowVisibility(lockIndex); });
                playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(lockHoverColor);


                EventTrigger.Entry endHoverColor = new EventTrigger.Entry();
				endHoverColor.eventID = EventTriggerType.PointerExit;
				endHoverColor.callback.AddListener( (eventData) => {
					if ( !/*connectVisibilityLock*/colorFlowVisibilityLock ) GameManager.Instance.GetGridManager().HideGridColorMask();
                    ToggleFlowVisibility(false);
                } );
				playerInteraction_UI.rightPanelColors[triggerIndex].triggers.Add(endHoverColor);
			}
			else 
			{
//				Debug.Log(playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.name);
				playerInteraction_UI.rightPanelColors[triggerIndex].gameObject.SetActive(false);
			}
		}

/* Question Mark Hint Setup */
		for(int hintIndex = 0; hintIndex < playerInteraction_UI.hintButtons.Length; hintIndex++)
		{
			HintConstructor h = playerInteraction_UI.hintButtons[hintIndex].hint;
			if(playerInteraction_UI.hintButtons[hintIndex].levelIds.Count == 0 || playerInteraction_UI.hintButtons[hintIndex].levelIds.Contains( GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id )) 
			{
				GameObject hintInstance = Instantiate( playerInteraction_UI.hint_button_prefab, playerInteraction_UI.hint_button_container ) as GameObject;
				hintInstance.transform.localScale = Vector3.one;
				Button instanceButton = hintInstance.GetComponent<Button>();
				Image instanceImage = hintInstance.GetComponent<Image>();
				instanceButton.onClick.AddListener( ()=> TriggerHint( h.hintTitle, h.hintDescription, h.hintImage ) );

				if(playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage!=null)
				{
					//	playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.GetComponent<Image>().sprite = playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage;
					instanceImage.sprite =  playerInteraction_UI.hintButtons[hintIndex].hint.hintButtonImage;
				}
				else if(playerInteraction_UI.hintButtons[hintIndex].hint.hintImage!=null)
				{
					//playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.GetComponent<Image>().sprite = playerInteraction_UI.hintButtons[hintIndex].hint.hintImage;
					instanceImage.sprite =  playerInteraction_UI.hintButtons[hintIndex].hint.hintImage;
				}
				//playerInteraction_UI.hintButtons[hintIndex].hintButtonTrigger.onClick.AddListener( ()=> TriggerHint( h.hintTitle, h.hintDescription, h.hintImage ) );	
			}
		}
        /* Toolbar Tooltips */
        foreach (TooltipEvent t in playerInteraction_UI.tooltipEvents)
        {
            EventTrigger.Entry beginHover_event = new EventTrigger.Entry();
            string tooltipText = t.tooltipContent.tooltipText;
            string tooltipName = t.tooltipUiElement.name;
            string refString = t.refString;
            beginHover_event.eventID = EventTriggerType.PointerEnter;
            if (t.permanent)
            {
                beginHover_event.callback.AddListener((eventData) => { if (interactionPhase == InteractionPhases.ingame_default) { playerInteraction_UI.tooltipOverlay.OpenPanel(); playerInteraction_UI.tooltipOverlay.SetTooltip(tooltipText, Input.mousePosition); GameManager.Instance.tracker.CreateEventExt("tooltip", tooltipName); } });
            }
            else
            {
                if(!PlayerPrefs.HasKey(refString) || PlayerPrefs.GetInt(refString) == 0)
                {
                    beginHover_event.callback.AddListener((eventData) => { if (interactionPhase == InteractionPhases.ingame_default) { playerInteraction_UI.tooltipOverlay.OpenPanel(); playerInteraction_UI.tooltipOverlay.SetTooltip(tooltipText, Input.mousePosition); GameManager.Instance.tracker.CreateEventExt("tooltip", tooltipName); PlayerPrefs.SetInt(refString, 1); beginHover_event.callback.RemoveAllListeners(); } });
                }
            }
            t.tooltipUiElement.triggers.Add(beginHover_event);
        }
    }

	public void TriggerHint(string title, string description, Sprite texture)
	{
		GameManager.Instance.tracker.CreateEventExt("hint",title);
		playerInteraction_UI.hintOverlay.OpenPanel();
		playerInteraction_UI.hintOverlay.SetHint(title,description,texture);
	}

	public void BeginDrag(MenuOptions selectedOption)
	{
		if(interactionPhase != InteractionPhases.ingame_default) return;

		Sprite[] spriteSheet = Resources.LoadAll<Sprite>("Sprites/gridsprites_v3") as Sprite[];
		GameManager.Instance.tracker.CreateEventExt("startDrag",selectedOption.ToString());

        if (onMenuInteraction != null) onMenuInteraction(selectedOption);
        switch (selectedOption)
		{
			case MenuOptions.semaphore:
				foreach(Sprite s in spriteSheet)
				{
					if(s.name == "semaphore")
					{
						playerInteraction_UI.SetDraggableElement( s );	
					}
				}
			break;
			case MenuOptions.button:
				foreach(Sprite s in spriteSheet)
				{
					if(s.name == "button_up")
					{
						playerInteraction_UI.SetDraggableElement( s );	
					}
				}
			break;
		}
        Vector3 draggableItemScale = Vector3.one;
        draggableItemScale *= 5f /* default ortho size */ / (GameManager.Instance.GetGridManager().worldCamera.orthographicSize) /* current ortho size */;
        playerInteraction_UI.draggableElement.transform.localScale = draggableItemScale;
    }
	public void ContinueDrag(MenuOptions selectedOption)
	{
		playerInteraction_UI.SetDraggableElementPosition(Input.mousePosition);
	}

	public void EndDrag(MenuOptions selectedOption)
	{
		if(interactionPhase != InteractionPhases.ingame_default) return;
		playerInteraction_UI.ReleaseDraggableElement();
		GameManager.Instance.tracker.CreateEventExt("startDrag",selectedOption.ToString());
		if( GameManager.Instance.GetGridManager().IsValidLocation(Input.mousePosition) && !GameManager.Instance.GetGridManager().IsOccupied(Input.mousePosition) ) 
		{ 
			GameManager.Instance.GetGridManager().PlaceGridElementAtLocation( Input.mousePosition, selectedOption );
        }
	}

	public void BeginHover(MenuOptions selectedOption)
	{
		GameManager.Instance.tracker.CreateEventExt("beginHover",selectedOption.ToString());
		switch(selectedOption)
		{
			case MenuOptions.trash:
			trashHover = true;
			break;
		}
	}
	public void EndHover(MenuOptions selectedOption)
	{
		GameManager.Instance.tracker.CreateEventExt("endHover",selectedOption.ToString());
		switch(selectedOption)
		{
			case MenuOptions.trash:
			trashHover = false;
			break;
		}
	}

	void ResetStartValues()
	{
		List<GridObjectBehavior> resetObjects = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>(){"thread","delivery","pickup","exchange","semaphore"});
		foreach(GridObjectBehavior resetObject in resetObjects)
		{
				resetObject.ResetPosition();
		}

		flowVisibility = false;

        if (connectVisibilityLock) { ToggleConnectionVisibility(); }
    }

    void TriggerSimulation(LinkJava.SimulationTypes simulationType)
    {
        if (tutorialMode)
        {
            interactionPhase = InteractionPhases.awaitingSimulation;
            playerInteraction_UI.loadingOverlay.OpenPanel();
            GameManager.Instance.PlayTutorialLevel();
        }
        else
        {
            interactionPhase = InteractionPhases.awaitingSimulation;
            playerInteraction_UI.loadingOverlay.OpenPanel();
            GameManager.Instance.SubmitCurrentLevel(simulationType);
        }
    }

    [ContextMenu("Reset Placed Objects")]
    void ResetPlacedObjects()
    {
        GameManager.Instance.GetGridManager().ClearGrid(false);
    }

    void TriggerTutorialSimulation()
    {

    }

	public void StartSimulation()
	{
		if(interactionPhase != InteractionPhases.awaitingSimulation) return;
        playerInteraction_UI.loadingOverlay.ClosePanel();
        interactionPhase = InteractionPhases.simulation;
        Debug.Log("Setting to Simulation.");
		GridObjectBehavior[] gridObjs = GameManager.Instance.GetGridManager().RetrieveComponentsOfType("thread");
		foreach(GridObjectBehavior g in gridObjs) g.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.thread_simulation;

		playerInteraction_UI.simulationButton.interactable = false;
		playerInteraction_UI.simulationButton.gameObject.SetActive(false);
		playerInteraction_UI.submitButton.interactable = false;
		//playerInteraction_UI.submitButton.gameObject.SetActive(false);
		playerInteraction_UI.stopSimulationButton.interactable = true;
		playerInteraction_UI.stopSimulationButton.gameObject.SetActive( true );

        //reset zoom stuff
        ResetZoom();


        simulationTime = 0f;
		playerInteraction_UI.goalOverlay.userInput = PlayerInteraction_UI.Goal_UIOverlay.UserInputs.none;
		StartCoroutine( SimulationBehavior() );
	}

	void EndSimulation()
	{
        Debug.Log("EndSimulation");
		StopCoroutine( SimulationBehavior() );

        tutorialMode = false;

        interactionPhase = InteractionPhases.ingame_default;

		ResetStartValues();

		playerInteraction_UI.simulationButton.interactable = true;
		playerInteraction_UI.simulationButton.gameObject.SetActive(true);
		playerInteraction_UI.submitButton.interactable = true;
		playerInteraction_UI.stopSimulationButton.interactable = false;
		playerInteraction_UI.stopSimulationButton.gameObject.SetActive(false);

	}
		
	void PlayerInteractionListener()
	{
		switch(interactionPhase)
		{
			case InteractionPhases.ingame_default:
				if(playerInteraction_UI.IsSubPanelOpen()) return;
                /*
                 * if player LEFT clicks during basic play, they can 
                 * (1) Click and drag movable elements
                */
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if (GameManager.Instance.GetGridManager().IsEditableElement(Input.mousePosition))
                    {
                        currentGridObject = GameManager.Instance.GetGridManager().RetrieveEditableGridObject(Input.mousePosition);
                        currentGridObject.BeginDrag();
                        interactionPhase = InteractionPhases.ingame_dragging;
                        GameManager.Instance.tracker.CreateEventExt("BeginReposition", currentGridObject.component.type);

                        if (currentGridObject.component.type == "signal" && connectVisibilityLock)
                        {
                            Signal_GridObjectBehavior s = (Signal_GridObjectBehavior)currentGridObject;
                            s.SetHighlight(false);
                        }
                    }
                    if (hoverObject)
                    {
                        if (!connectVisibility) hoverObject.EndHoverBehavior();
                        hoverObject = null;
                    }
                }
                /*
                * if player RIGHT clicks during basic play, they can:
                * (1) link connectable elements through Signals
                * (2) Open/Close Semaphores
               */
                else if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    if (GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "signal") && GameManager.Instance.GetGridManager().IsEditableElement( Input.mousePosition ) )
                    {
                        currentGridObject = GameManager.Instance.GetGridManager().RetrieveGridObjectOfType(Input.mousePosition, "signal");
                        currentGridObject.EnableGridObjectEventBehaviors(GridObjectBehavior.InteractTypes.rightClick);
                        interactionPhase = InteractionPhases.ingame_connecting;
                        currentGridObject.BeginInteraction();

                        List<GridObjectBehavior> otherSignals = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>() { "signal" });
                        foreach (GridObjectBehavior otherSignal in otherSignals)
                        {
                            if (currentGridObject != otherSignal) { otherSignal.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.connectionOverlay - 1; }
                        }

                        GameManager.Instance.tracker.CreateEventExt("BeginLink", currentGridObject.component.type);

                        playerInteraction_UI.onHoverLightbox.OpenPanel();

                    }
                    else if (GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "semaphore") && GameManager.Instance.GetGridManager().IsEditableElement(Input.mousePosition))
                    {
                        currentGridObject = GameManager.Instance.GetGridManager().RetrieveGridObjectOfType(Input.mousePosition, "semaphore");
                        currentGridObject.EnableGridObjectEventBehaviors(GridObjectBehavior.InteractTypes.rightClick);
                        currentGridObject.BeginInteraction();
                        GameManager.Instance.tracker.CreateEventExt("BeginLink", currentGridObject.component.type);
                    }

                    if (hoverObject /*&& !connectVisibilityLock*/)
                    {
                        hoverObject.EndHoverBehavior();
                        hoverObject = null;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Mouse2))
                {
                    ResetZoom();
                }
                /*
                 * if a player isn't clicking the mouse, we should check for hover behaviors AND zoom behaviors
                */
                else
                {
                    if (Input.mousePosition == stationaryMousePosition) //if mouse is stationary
                    {
                        if (hoverObject == null)
                        {
                            stationaryTime += Time.deltaTime;
                            if (stationaryTime >= 0.2f)
                            {
                                if (
                                        GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "signal")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "diverter")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "exchange")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "delivery")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "pickup")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "conditional")
                                        || GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "semaphore")
                                    )
                                {
                                    hoverObject = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
                                    hoverObject.OnHoverBehavior();
                                    GameManager.Instance.tracker.CreateEventExt("OnHoverBehavior", hoverObject.component.type);
                                }
                            }
                        }

                        if (!isZooming)
                        {
                            float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
                            if (scrollAxis < -0.05f) TriggerZoomOut();
                            else if (scrollAxis > 0.05f) TriggerZoomIn();
                        }
                    }
                    else //if mouse has moved since last frame 
                    {
                        stationaryMousePosition = Input.mousePosition;
                        if (hoverObject)
                        {
                            if (GameManager.Instance.GetGridManager().IsOccupied(Input.mousePosition))
                            {
                                if (hoverObject != GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition))
                                { EndHoverEvent(); }
                            }
                            else { EndHoverEvent(); }
                        }
                        else
                        {
                            stationaryTime = 0f;
                        }

                        //pop up tooltip close check
                        if (playerInteraction_UI.tooltipOverlay.tooltipActive && Time.time - playerInteraction_UI.tooltipOverlay.openTime > 0.5f)
                        { playerInteraction_UI.tooltipOverlay.ClosePanel(); }
                    }
                    stationaryMousePosition = Input.mousePosition;
                    GridObjectBehavior hoverObject__ = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
                    if (hoverObject__ != null && hoverObject__ != hoverObject_)
                    {
                        hoverObject_ = hoverObject__;
                        if (hoverObject_.component != null)
                        {
                            GameManager.Instance.tracker.CreateEventExt("OnMouseComponent", hoverObject_.component.type.ToString() + "/" + hoverObject_.component.id.ToString());
                        }
                    }
                    if (hoverObject__ == null && hoverObject_ != null)
                    {
                        GameManager.Instance.tracker.CreateEventExt("OutMouseComponent", hoverObject_.component.type.ToString() + "/" + hoverObject_.component.id.ToString());
                        hoverObject_ = null;
                    }

                }
			break;
		case InteractionPhases.ingame_dragging:
			if(Input.GetKey(KeyCode.Mouse0))
			{
				if( currentGridObject != null )
				{
					currentGridObject.ContinueDrag();
					if(trashHover) { }
					else { }
				}
				else 
				{
                        Debug.Log("END DRAGGING");
                        interactionPhase = InteractionPhases.ingame_default;
				}
			}
			else 
			{
				if(trashHover)
				{
					GameManager.Instance.GetGridManager().ForgetGridElement( currentGridObject );
					if( currentGridObject.component.configuration.link != null && currentGridObject.component.configuration.link > 0 )
					{
					//	GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByID( currentGridObject.component.configuration.link );
					//	g.component.configuration.link = 0;
					}
					GameManager.Instance.tracker.CreateEventExt("Destroying",currentGridObject.component.type);	
					Destroy( currentGridObject.gameObject );
					currentGridObject = null;

                        Debug.Log("END TRASH HOVER");
                        interactionPhase = InteractionPhases.ingame_default;
					
				}
				else 
				{
					GameManager.Instance.tracker.CreateEventExt("EndReposition",currentGridObject.component.type);	
					currentGridObject.EndDrag();

                    if (currentGridObject.component.type == "signal" && connectVisibilityLock)
                    {
                        Signal_GridObjectBehavior s = (Signal_GridObjectBehavior)currentGridObject;
                        s.SetHighlight(true);
                    }

                    currentGridObject = null;
                        
                    Debug.Log("END REPOSITION");
					interactionPhase = InteractionPhases.ingame_default;
                }


			}
		break;
		case InteractionPhases.ingame_connecting:

			if(Input.GetKeyDown(KeyCode.Mouse1))
			{
				currentGridObject.EndInteraction();
				if( GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "semaphore") ) 
				{
					GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
					currentGridObject.LinkTo( g );
					GameManager.Instance.tracker.CreateEventExt("LinkTo",currentGridObject.component.type);
				}

				else if( GameManager.Instance.GetGridManager().IsObjectOfType(Input.mousePosition, "conditional") ) 
				{
					GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByMousePosition(Input.mousePosition);
					currentGridObject.LinkTo( g );
					GameManager.Instance.tracker.CreateEventExt("LinkTo",currentGridObject.component.type);
				}

				playerInteraction_UI.onHoverLightbox.ClosePanel();

				List<GridObjectBehavior> otherSignals = GameManager.Instance.GetGridManager().GetGridComponentsOfType(new List<string>(){"signal"});
				foreach(GridObjectBehavior otherSignal in otherSignals)
				{
					if(currentGridObject != otherSignal ) { otherSignal.GetComponent<SpriteRenderer>().sortingOrder = Constants.ComponentSortingOrder.connectionComponents;}
					if(connectVisibilityLock) otherSignal.SetHighlight( true );
				}

                
                Debug.Log("END CONNECTING");
				interactionPhase = InteractionPhases.ingame_default;
			}
			else 
			{
				currentGridObject.ContinueInteraction();
			}
		break;
		case InteractionPhases.simulation:
			simulationTime+=Time.deltaTime;
		break;
		}

	}

    //this makes the level do
	IEnumerator SimulationBehavior()
	{
		Level lvl = GameManager.Instance.GetDataManager().currentLevelData;
        Debug.Log(lvl.execution.Count);
        int currentStep = 0;
		int maxStep = 0;
		int maxGoalsCompleted = 0;
        bool nextLevelButtonVisibility = false;
		Dictionary<int,List<StepData>> stepDictionary = new Dictionary<int, List<StepData>>();
		Dictionary<int,List<int>> componentStepsDictionary = new Dictionary<int,List<int>>();

		for( int i = 0; i < lvl.execution.Count; i++ ) 
		{
			StepData step = lvl.execution[i];

			if(step.timeStep > maxStep)
			{
				maxStep = step.timeStep;
			}

			if( step.eventType == "M") 
			{
				if( !componentStepsDictionary.ContainsKey( step.componentID ) ) { componentStepsDictionary.Add(step.componentID, new List<int>()); componentStepsDictionary[step.componentID].Add(i); }
				else { componentStepsDictionary[step.componentID].Add(i); }
			}

			if( stepDictionary.ContainsKey( step.timeStep ) )
			{
				if(step.eventType=="D")
				{
					stepDictionary[ step.timeStep ].Insert( 0, step );
				}
				else
				{
					stepDictionary[ step.timeStep ].Add( step );	
				}
			}
			else 
			{
				stepDictionary[ step.timeStep ] = new List<StepData>();
				stepDictionary[ step.timeStep ].Add( step );
			}
		}

		foreach( int componentId in componentStepsDictionary.Keys )
		{
			//componentStepsDictionary[componentId].Sort();
			for(int listIndex = 0; listIndex < componentStepsDictionary[componentId].Count-1; listIndex++)
			{
				int executionIndex = componentStepsDictionary[componentId][listIndex];
				int nextExecutionIndex = componentStepsDictionary[componentId][listIndex+1];
				lvl.execution[executionIndex].SetNextStep(nextExecutionIndex);
			}
		}
			
		while(interactionPhase == InteractionPhases.simulation && currentStep <= maxStep)
		{
			if( stepDictionary.ContainsKey(currentStep) ) 
			{
                float waitTime = 0f;
                int count = 0;
				foreach( StepData step in stepDictionary[currentStep] )
				{
                    count++;
					if(step.componentID==0)
					{
                        yield return new WaitForSeconds(0.5f);
						if(step.componentStatus == null) continue;
						if(step.componentStatus.goals_completed != null && step.componentStatus.final_condition != -1)
						{
							if(maxGoalsCompleted < step.componentStatus.goals_completed) { maxGoalsCompleted = step.componentStatus.goals_completed; }
						}
						if(step.componentStatus.final_condition != null && step.componentStatus.final_condition != -1)
						{
                            score.stepCount = maxStep;
							string titleFormatString = "<size=18><b>{0}</b></size>\n";
							string titleString = "";
							string goalString = "";
                            string levelFileName = "";
                            if(GameManager.Instance.currentLevelReferenceObject!=null) levelFileName = GameManager.Instance.currentLevelReferenceObject.file;
                            switch (step.componentStatus.final_condition)
							{
							case 2:
							case 8:
							case 10:
                                    //if "test" versus "submit" change this text
                                    if (GameManager.Instance.GetCurrentSimulationType() == LinkJava.SimulationTypes.ME)
                                    {
										titleString = "SUCCESSFUL SOLUTION";
                                        goalString += "\n• Congratulations! This solution will always work. Please proceed to the next level.";
                                        score.completed = true;

                                        //get current score
                                        int currentScore = GameManager.Instance.GetScoreManager().GetCalculatedScore(score);
                                        //update saved score
                                        GameManager.Instance.GetScoreManager().ScoreLevel(score);
                                        int lvlScore = GameManager.Instance.GetScoreManager().GetCalculatedScore(score.index);

                                        GameManager.Instance.currentLevelReferenceObject.completionRank = lvlScore;
                                        GameManager.Instance.GetDataManager().UpdateLevelRank(levelFileName, lvlScore);

                                        //use 'current' not 'best' score for the feedback
                                        playerInteraction_UI.goalOverlay.SetFeedbackScore(currentScore);

                                        nextLevelButtonVisibility = true;
                                    }
                                    else if (GameManager.Instance.GetCurrentSimulationType() == LinkJava.SimulationTypes.Play)
									{
										titleString = "TEST COMPLETE";
                                        goalString += "\n• This solution was successful this time. Submit to check if it's always successful.";
                                        playerInteraction_UI.goalOverlay.SetFeedbackScore(-1);
                                    }
								break;
							default:
								//if "test" versus "submit" change this text
								if (GameManager.Instance.GetCurrentSimulationType() == LinkJava.SimulationTypes.ME)
								{
									titleString = "UNSUCCESSFUL SOLUTION";
								}
								else if (GameManager.Instance.GetCurrentSimulationType() == LinkJava.SimulationTypes.Play)
								{
									titleString = "TEST COMPLETE";
                                    playerInteraction_UI.goalOverlay.SetFeedbackScore(-1);
                                    }

								goalString = "";
								if ((step.componentStatus.final_condition & 1)!=0) {
									goalString += "• Make sure arrows aren't blocked.\n";
								}
								if ((step.componentStatus.final_condition & 4)!=0) {
									goalString += "• This solution was unsuccessful.\n";
								}
								if ((step.componentStatus.final_condition & 16)!=0) {
									goalString += "• Make sure arrows can't deliver at the same time.\n";
								}
								if ((step.componentStatus.final_condition & 32)!=0) {
									goalString += "• Make sure all arrows can move.\n";
								}
								if ((step.componentStatus.final_condition & 64)!=0) {
									goalString += "• Make sure arrows don't get caught in an infinite loop.\n";
								}
								if ((step.componentStatus.final_condition & 512)!=0) {
									goalString += "• Wrong turn! Check the Flow Arrows at the top of the screen.\n";
								}
                                List<string> errorFeedback = new List<string>();
                                foreach (string errorKey in step.componentStatus.goal_descriptions)
                                {
                                    string key = errorKey.Substring(0, 3);
                                    if (Constants.GoalFeedbackValues.GoalErrorFeedback.ContainsKey(key))
                                    {
                                        if( !errorFeedback.Contains( Constants.GoalFeedbackValues.GoalErrorFeedback[key] ))
                                            errorFeedback.Add(Constants.GoalFeedbackValues.GoalErrorFeedback[key]);
                                    }
                                }
                                foreach (string s in errorFeedback) goalString += ("• " + s + "\n");

                                break;
							}

							goalString = string.Format(titleFormatString, titleString) + goalString;
                            
							if (GameManager.Instance.GetCurrentSimulationType () == LinkJava.SimulationTypes.ME) {
								playerInteraction_UI.goalOverlay.levels.gameObject.SetActive(true);
							} else if (GameManager.Instance.GetCurrentSimulationType() == LinkJava.SimulationTypes.Play) {
								playerInteraction_UI.goalOverlay.levels.gameObject.SetActive(false);
							}

							yield return StartCoroutine( playerInteraction_UI.TriggerGoalPopUp(goalString) );
						}
					}
					else
					{
						GridObjectBehavior g = GameManager.Instance.GetGridManager().GetGridObjectByID( step.componentID );
						if(g!=null) 
						{
							float time = g.DoStep( step );
                            if (time > waitTime)
                                waitTime = time;
						}
						else { Debug.Log("Could not find " + step.componentID); }
					}

                    if (onSimulationStep != null)
                    {
                        onSimulationStep(step);
                        //PauseSimulation();
                        //UnpauseAfterDelay(5);
                    }
                }
                while (paused) yield return new WaitForSeconds(0.1f);
				yield return new WaitForSeconds(waitTime);
			}
			currentStep ++;
			//Debug.Log(currentStep);
		}

        if (onCompletion != null) onCompletion();

		//yield return new WaitForSeconds(1f);
		//todo: switch statement of the selected goal option

		ResetStartValues();

		switch( playerInteraction_UI.goalOverlay.userInput )
		{
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.exit:
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.levels:
				TriggerPlayPhaseEnd();
                Debug.Log("User input for exit or levels hit.");
                EndSimulation();
			    break;
            case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.stop:
                TriggerPlayPhaseEnd();
                Debug.Log("User input for exit or levels hit.");
                EndSimulation();
                break;
			case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.replay:

                Debug.Log("REPLAY");
                //TODO: CHECK IF INTERACTION PHASE IS INCORRECT HERE.
                interactionPhase = InteractionPhases.awaitingSimulation;
				GameManager.Instance.TriggerLevelSimulation( LinkJava.SimulationFeedback.none );

			break;
            case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.retry:
                Debug.Log("Retry");
                interactionPhase = InteractionPhases.ingame_default;
                EndSimulation();
                GameManager.Instance.TriggerLevelTutorial
                (
                    GameManager.Instance.GetDataManager().currentLevelData.metadata.level_id,
                    interactionPhase == InteractionPhases.awaitingSimulation || interactionPhase == InteractionPhases.simulation ? TutorialEvent.TutorialInitializeTriggers.duringSimulation : TutorialEvent.TutorialInitializeTriggers.beforePlay
                );
                break;
            case PlayerInteraction_UI.Goal_UIOverlay.UserInputs.levelsNext:
                TriggerPlayPhaseEnd(GameManager.GamePhases.LoadScreen, true);
                break;
			default:
                Debug.Log("No case defined for " + playerInteraction_UI.goalOverlay.userInput.ToString());
                interactionPhase = InteractionPhases.ingame_default;
			    break;
		}
	}



	public void ToggleFlowVisibility()
	{
		flowVisibility = !flowVisibility;
		GameManager.Instance.tracker.CreateEventExt("ToggleFlowVisibility",flowVisibility.ToString());

		GridObjectBehavior[] tracks = GameManager.Instance.GetGridManager().RetrieveTracks();//.RetrieveComponentsOfType("intersection");
		foreach(GridObjectBehavior track in tracks)
		{
			if(flowVisibility) { track.BeginInteraction(); }
			else { track.EndInteraction(); }
		}
	}
	public void ToggleFlowVisibility(bool setVisibility)
	{
		if(setVisibility == flowVisibility) return;
		if (setVisibility == false && /*connectVisibilityLock*/colorFlowVisibilityLock) return;
		flowVisibility = setVisibility;
		GameManager.Instance.tracker.CreateEventExt("ToggleFlowVisibility",flowVisibility.ToString());

		GridObjectBehavior[] tracks = GameManager.Instance.GetGridManager().RetrieveTracks();//.RetrieveComponentsOfType("intersection");
		foreach(GridObjectBehavior track in tracks)
		{
			if(flowVisibility) { track.BeginInteraction(); }
			else { track.EndInteraction(); }
		}
	}

    void LockFlowVisibility(int lockTarget)
    {
    	GameManager.Instance.tracker.CreateEventExt("LockFlowVisibility",lockTarget.ToString());
        if (lockTarget == -1) //force quit
        {
            playerInteraction_UI.rightPanelColorLock.enabled = false;
			colorFlowVisibilityLock = false;
            return;
        }
		colorFlowVisibilityLock = !colorFlowVisibilityLock;
		if (/*connectVisibilityLock*/colorFlowVisibilityLock)
        {
            playerInteraction_UI.rightPanelColorLock.rectTransform.position = playerInteraction_UI.rightPanelColors[lockTarget].transform.position;
            playerInteraction_UI.rightPanelColorLock.enabled = true;
        }
        else
        {
            playerInteraction_UI.rightPanelColorLock.enabled = false;
            GameManager.Instance.GetGridManager().RevealGridColorMask(lockTarget);
        }
    }


    public void ToggleConnectionVisibility()
	{
		connectVisibility = !connectVisibility;

		if(connectVisibilityLock && !connectVisibility) return;

		GameManager.Instance.tracker.CreateEventExt("ToggleConnectionVisibility",connectVisibility.ToString());

		GridObjectBehavior[] gridObjects = GameManager.Instance.GetGridManager().RetrieveComponentsOfType("signal");
		foreach(GridObjectBehavior g in gridObjects)
		{
			Signal_GridObjectBehavior s = (Signal_GridObjectBehavior) g;
			s.SetHighlight(connectVisibility);
		}
	}
	void LockConnectionVisibility()
	{
		connectVisibilityLock = !connectVisibilityLock;
		GameManager.Instance.tracker.CreateEventExt("LockConnectionVisibility",connectVisibilityLock.ToString());
		playerInteraction_UI.topPanelConnectionLock.enabled =  connectVisibilityLock;
	}

	public void TogglePauseMenu()
	{
		if(playerInteraction_UI.pauseOverlay.isPaused)
		{
			playerInteraction_UI.pauseOverlay.ClosePanel();	
			GameManager.Instance.tracker.CreateEventExt("ClosePausePanel","");
		}
		else 
		{
			playerInteraction_UI.pauseOverlay.OpenPanel();
			GameManager.Instance.tracker.CreateEventExt("OpenPausePanel","");
		}
	}

	void ToggleHintsVisibility()
	{
		GameManager.Instance.tracker.CreateEventExt("ToggleHintsVisibility",(!playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf).ToString());
		if(playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf) { playerInteraction_UI.hintOverlay.ClosePanel(); }
		playerInteraction_UI.UIOverlay_Hint_Container.gameObject.SetActive( !playerInteraction_UI.UIOverlay_Hint_Container.gameObject.activeSelf );

	}

    void EndHoverEvent()
    {
        if ( connectVisibilityLock || hoverObject==null ) return;
        stationaryTime = 0f;
        hoverObject.EndHoverBehavior();
        GameManager.Instance.tracker.CreateEventExt("EndHoverBehavior", hoverObject.component.type);
        hoverObject = null;
    }

	public void TriggerPlayPhaseEnd( GameManager.GamePhases nextPhase  = GameManager.GamePhases.LoadScreen, bool autoOpenNextLevel = false )
	{
		if(interactionPhase == InteractionPhases.simulation) 
		{
			StopAllCoroutines();
			EndSimulation();
		}
        score.attemptCount = 0;
        tutorialMode = false;
		GameManager.Instance.SetGamePhase( nextPhase );
        if (autoOpenNextLevel)
        {
            GameManager.Instance.TriggerAdvanceToNextLevel();
        }
	}

    void TriggerZoomIn()
    {
        if (zoomLevel >= maxZoomLevel) return;
        isZooming = true;
        StartCoroutine(ZoomCooldownCoroutine());
        zoomLevel++;
		playerInteraction_UI.zoomMeter.SetMeterValue( zoomLevel / maxZoomLevel );
        //Debug.Log("Zoom in triggered.");
        UpdateZoom();
    }
    void TriggerZoomOut()
    {
        if (zoomLevel <= 0f) return;
        isZooming = true;
        StartCoroutine(ZoomCooldownCoroutine());
        zoomLevel--;
		playerInteraction_UI.zoomMeter.SetMeterValue( zoomLevel / maxZoomLevel );
        //Debug.Log("Zoom out triggered.");
        UpdateZoom();
    }

    void UpdateZoom()
    {
        zoomLevel = Mathf.Clamp(zoomLevel, 0f, maxZoomLevel);
        MoveCameraToMousePosition();
        currentOrthographicSize = originalOrthographicSize - ((originalOrthographicSize * zoomLevel / maxZoomLevel) * 0.5f);
        //Debug.Log("Should zoom to: " + currentOrthographicSize);
        //GameManager.Instance.GetGridManager().worldCamera.orthographicSize = currentOrthographicSize;
        float fromOrthoSize = GameManager.Instance.GetGridManager().worldCamera.orthographicSize;
        StartCoroutine(ZoomOrthographicSizeCoroutine(fromOrthoSize, currentOrthographicSize));
    }
    
    //TODO: Grid Manager should probably be in charge of Camera position
    void MoveCameraToMousePosition()
    {
        Vector3 newTargetPosition = GameManager.Instance.GetGridManager().worldCamera.ScreenToWorldPoint(Input.mousePosition);
        if (zoomLevel == 0f) newTargetPosition = originalCameraPosition;
        newTargetPosition.z = GameManager.Instance.GetGridManager().worldCamera.transform.position.z;
        iTween.MoveTo(GameManager.Instance.GetGridManager().worldCamera.gameObject, newTargetPosition, 1f);
        currentCameraPosition = newTargetPosition;
    }

    IEnumerator ZoomCooldownCoroutine()
    {
        yield return new WaitForSeconds (0.5f);
        isZooming = false;
    }

    IEnumerator ZoomOrthographicSizeCoroutine(float fromOrthosize, float toOrthosize)
    {
        Camera orthoCam = GameManager.Instance.GetGridManager().worldCamera;
        float timeToZoom = 0.5f;
        while (timeToZoom > 0f)
        {
            timeToZoom -= Time.deltaTime;
            float percentage = 1f - (timeToZoom / 0.5f);

            orthoCam.orthographicSize = toOrthosize + (fromOrthosize - toOrthosize) *(timeToZoom/0.5f);

            yield return null;
        }

        yield return null;
    }

    void ResetZoom()
    {
        zoomLevel = 0f;
        currentOrthographicSize = originalOrthographicSize;
        currentCameraPosition = originalCameraPosition;
        GameManager.Instance.GetGridManager().worldCamera.orthographicSize = originalOrthographicSize;
        GameManager.Instance.GetGridManager().worldCamera.transform.position = originalCameraPosition;
		playerInteraction_UI.zoomMeter.SetMeterValue( zoomLevel / maxZoomLevel );
    }

    void PauseSimulation()
    {
        paused = true;
		GameManager.Instance.tracker.CreateEventExt("PauseSimulation",paused.ToString());
    }

    void UnpauseSimulation()
    {
        paused = false;
		GameManager.Instance.tracker.CreateEventExt("PauseSimulation",paused.ToString());
    }

    void DelayedUnpause(float delay = 0, TutorialEvent t = null)
    {
        Debug.Log(delay);
        if (delay == 0)
        {
            paused = false;
			GameManager.Instance.tracker.CreateEventExt("PauseSimulation",paused.ToString());
        }
        else
        {
            StartCoroutine(DelayedUnpauseRoutine(delay, t));
        }
    }

    IEnumerator DelayedUnpauseRoutine(float delay, TutorialEvent t)
    {
        yield return new WaitForSeconds(delay);
        paused = false;
        if(t != null) GameManager.Instance.ReportTutorialEventComplete(t);
		GameManager.Instance.tracker.CreateEventExt("PauseSimulation",paused.ToString());
    }
}


