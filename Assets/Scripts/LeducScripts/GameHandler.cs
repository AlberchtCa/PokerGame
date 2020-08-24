using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.IO;

public class GameHandler : MonoBehaviour
{
    public GameObject hero_card;
    public GameObject opponent_card;
    public GameObject community_card;
    public List<Sprite> card_sprites;
    public GameObject call_button_holder;
    public GameObject check_button_holder;
    public GameObject next_hand_button_holder;
    public GameObject fold_button_holder;
    public GameObject raise_button_holder;
    public GameObject bet_button_holder;
    public GameObject replay_button_holder;
    public Button replay_button;
    public Button fold_button;
    public Button call_button;
    public Button raise_button;
    public Button check_button;
    public Button bet_button;
    public Button next_hand_button;
    public Text pot;
    public Text score;
    public TextMeshProUGUI log;
    public string hero_card_r;
    public string opponent_card_r;
    public string community_card_r;

    private List<int> deck_indexes;
    private List<string> deck;
    private int first_to_act;
    private string history;
    private int re_raise_param;
    private int street;
    private string last_action;
    private char[] actions = { 'C', 'B', 'c', 'R', 'F' };
    private int acting_player;
    private Dictionary<string, int> ranks;
    private Dictionary<string, string> actions_e;

    //for hand replays
    public GameObject next_action_button_holder;
    public GameObject previous_action_button_holder;
    public GameObject strategy_holder;
    public Button next_action_button;
    public Button previous_action_button;
    public TextMeshProUGUI strategy_text;

    private int action_replay_index;
    private bool is_simulation;
    private string simulation_history;

    //Hand history part
    void ModifyHistoryButtons(bool activity)
    {
        next_action_button_holder.SetActive(activity);
        previous_action_button_holder.SetActive(activity);
        strategy_holder.SetActive(activity);
    }

    public void ReplayLastHand()
    {
        ModifyActionButtons(false);
        ModifyHistoryButtons(true);
        next_hand_button_holder.SetActive(true);
        replay_button_holder.SetActive(false);
        action_replay_index = 0;
        acting_player = (first_to_act + 1) % 2;
        street = 0;
        re_raise_param = 0;
        last_action = "none";
        is_simulation = true;
        InitializeSimulation();
    }

    void SimulateHand(string current_history)
    {
        InitializeSimulation();
        foreach(char action in current_history)
        {
            if (action == 'c')
                HandleCheck();
            if (action == 'B')
                HandleBet();
            if (action == 'C')
                HandleCall();
            if (action == 'R')
                HandleRaise();
            if (action == 'F')
                HandleFold();
        }
    }

    public void NextActionReplay()
    {
        if (action_replay_index < history.Length)
        {
            action_replay_index += 1;
            SimulateHand(history.Substring(0, action_replay_index));

        }
    }

    public void PreviousActionReplay()
    {
        if(action_replay_index != 0)
        {
            action_replay_index -= 1;
            SimulateHand(history.Substring(0, action_replay_index));

        }
    }

    void InitializeSimulation()
    {
        simulation_history = "";
        pot.text = "Pot: 0";
        opponent_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[1]];
        community_card.SetActive(false);
        log.text = "";
        street = 0;
        last_action = "none";
        string player;
        if (first_to_act == 1)
            player = "Hero";
        else
            player = "Villain";
        acting_player = (first_to_act + 1) % 2;
        log.text += player + " is first to act." + '\n';
        strategy_text.text = "Call Bet Check Raise Fold";
    }


    //Game Part

    public void HandleCheck()
    {
        if (last_action == "none")
        {
            HandleActionHelper("c", false, false, false);
        }
        else
        {
            if (street == 1)
                HandleActionHelper("c", false, false, true);
            else
                HandleActionHelper("c", false, true, false);
        }
    }

    public void HandleBet()
    {
        HandleActionHelper("B", true, false, false);
    }

    public void HandleCall()
    {
        if (street == 1)
            HandleActionHelper("C", true, false, true);
        else
            HandleActionHelper("C", true, true, false);
    }

    public void HandleRaise()
    {
        re_raise_param += 1;
        HandleActionHelper("R", true, false, false);
    }

    public void HandleFold()
    {
        HandleActionHelper("F", false, false, true);
    }

    public void HandleNextHand()
    {
        InitializeGame();
    }

    void HandleActionHelper(string action, bool pot_updated, bool new_street, bool is_game_over)
    {
        if(is_simulation)
            strategy_text.text = "Call Bet Check Raise Fold\n";
        
        string player;
        if (acting_player == 0)
            player = "Hero";
        else
            player = "Villain";
        log.text += player + " " + actions_e[action] + '\n';
        last_action = action;
        if (!is_simulation)
            history += action;
        else
            simulation_history += action;
        if (pot_updated)
            UpdatePot();
        if (action == "F")
        {
            string in_pot = pot.text;
            in_pot = in_pot.Substring(in_pot.IndexOf(": ") + 2);
            in_pot = (Int32.Parse(in_pot) - 2 - 2 * street).ToString();
            pot.text = "Pot: " + in_pot;
        }

        if (new_street)
        {
            last_action = "none";
            street += 1;
            re_raise_param = 0;
            community_card.SetActive(true);
            community_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[2]];

        }

        if (is_game_over)
        {
            GameOver();
        }
        else
        {
            acting_player = (acting_player + 1) % 2;
            if (!is_simulation)
            {
                SetPossibleButtons();
                if (acting_player == 1)
                {
                    MakeAllButtonsUnusable();
                    DecideActionAI();
                }
            }
            else
            {
                if (acting_player == 1)
                {
                    Debug.Log("Street: " + street);
                    List<float> odds = new List<float>();
                    odds = GetOddsAI(simulation_history);
                    strategy_text.text = "Call Bet Check Raise Fold\n";
                    strategy_text.text +=
                        odds[0].ToString() + "     " +
                        odds[1].ToString() + "   " +
                        odds[2].ToString() + "    " +
                        odds[3].ToString() + "   " +
                        odds[4].ToString();
                }
            }
        }

        if (is_simulation)
        {
            if(street == 1)
                community_card.SetActive(true);
        }
        

    }

    void UpdatePot()
    {
        string in_pot = pot.text;
        in_pot = in_pot.Substring(in_pot.IndexOf(": ") + 2);
        in_pot = (Int32.Parse(in_pot) + 2 + 2 * street).ToString();
        pot.text = "Pot: " + in_pot;
    }

    void GameOver()
    {
        string overall_score = score.text;
        overall_score = overall_score.Substring(overall_score.IndexOf(": ") + 2);
        string in_pot = pot.text;
        in_pot = in_pot.Substring(in_pot.IndexOf(": ") + 2);

        if (last_action == "F")
        {
            if (!is_simulation)
            {
                if (acting_player == 0)
                    overall_score = (Int32.Parse(overall_score) - Int32.Parse(in_pot)).ToString();
                else
                    overall_score = (Int32.Parse(overall_score) + Int32.Parse(in_pot)).ToString();
            }
        }
        else
        {
            opponent_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[1]];
            community_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[2]];
            int winner = DetermineWinner();
            if (winner == 0)
            {
                overall_score = (Int32.Parse(overall_score) + Int32.Parse(in_pot)).ToString();
                log.text += "Hero wins!";
            }
            else if (winner == 1)
            {
                overall_score = (Int32.Parse(overall_score) - Int32.Parse(in_pot)).ToString();
                log.text += "Hero loses..";
            }
            else
                log.text += "It's a tie.";
        }

        if (!is_simulation)
        {
            score.text = "Score: " + overall_score;

            first_to_act = (first_to_act + 1) % 2;
            ModifyActionButtons(false);
            replay_button_holder.SetActive(true);
            next_hand_button_holder.SetActive(true);
        }
    }


    int DetermineWinner()
    {
        string hero_cards = deck[deck_indexes[0]][0].ToString()
                            + deck[deck_indexes[2]][0].ToString();
        string opp_cards = deck[deck_indexes[1]][0].ToString()
                            + deck[deck_indexes[2]][0].ToString();

        int rank_hero = ranks[hero_cards];
        int rank_opp = ranks[opp_cards];

        if (rank_hero < rank_opp)
            return 0;
        if (rank_hero > rank_opp)
            return 1;
        return -1;


    }

    List<string> GetDeck()
    {
        List<string> deck = new List<string>();
        //J = 0, Q = 1, K = 2
        //The suits are clubs (c), diamonds (d), hearts (h) and spades (s)
        string[] cards = { "J", "Q", "K" };
        string[] suits = { "c", "d", "h", "s" };
        foreach (string card in cards)
            foreach (string suit in suits)
            {
                deck.Add(card + suit);
            }

        return deck;
    }

    List<int> GetCardIndexes()
    {
        List<int> deck_indexes = new List<int>();
        System.Random random = new System.Random();
        int i = 0;
        int curr_number = 0;
        while (i != 3)
        {
            curr_number = random.Next(0, 12);
            if (!deck_indexes.Contains(curr_number))
            {
                deck_indexes.Add(curr_number);
                i++;
            }
        }
        return deck_indexes;
    }

    void MakeAllButtonsUnusable()
    {
        fold_button.interactable = false;
        check_button.interactable = false;
        raise_button.interactable = false;
        call_button.interactable = false;
        bet_button.interactable = false;
        call_button_holder.SetActive(false);
        check_button_holder.SetActive(false);
    }

    void ModifyActionButtons(bool activity)
    {
        fold_button_holder.SetActive(activity);
        check_button_holder.SetActive(activity);
        raise_button_holder.SetActive(activity);
        call_button_holder.SetActive(activity);
        bet_button_holder.SetActive(activity);
        call_button_holder.SetActive(activity);
        check_button_holder.SetActive(activity);
    }

    void SetPossibleButtons()
    {
        MakeAllButtonsUnusable();
        if (last_action == "none" || last_action == "c")
        {
            check_button.interactable = true;
            check_button_holder.SetActive(true);
            bet_button.interactable = true;
        }

        if (last_action == "B")
        {
            raise_button.interactable = true;
            call_button.interactable = true;
            call_button_holder.SetActive(true);
            fold_button.interactable = true;
        }

        if (last_action == "R" && re_raise_param < 2)
        {
            raise_button.interactable = true;
            call_button.interactable = true;
            call_button_holder.SetActive(true);
            fold_button.interactable = true;
        }

        if (last_action == "R" && re_raise_param == 2)
        {
            call_button_holder.SetActive(true);
            call_button.interactable = true;
            fold_button.interactable = true;
        }
    }

    void InitializeGame()
    {
        is_simulation = false;
        strategy_holder.SetActive(false);
        ModifyActionButtons(true);
        log.text = "";
        next_hand_button_holder.SetActive(false);
        replay_button_holder.SetActive(false);
        previous_action_button_holder.SetActive(false);
        next_action_button_holder.SetActive(false);
        //set the community card and the opponent's card face down
        community_card.GetComponent<SpriteRenderer>().sprite = card_sprites[12];
        opponent_card.GetComponent<SpriteRenderer>().sprite = card_sprites[12];
        //get the three cards that will be played
        deck_indexes = GetCardIndexes();
        //associate indexes with cards
        hero_card_r = deck[deck_indexes[0]];
        opponent_card_r = deck[deck_indexes[1]];
        community_card_r = deck[deck_indexes[2]];
        //the first card is hero's card, the second one is the opponent's card
        //and the third one is the community card
        hero_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[0]];
        //set the history to empty for AI
        history = "";
        //set acting player
        acting_player = first_to_act;
        //set street
        street = 0;
        //set last action
        last_action = "none";
        //set reraise parameter
        re_raise_param = 0;
        //set pot to 0
        pot.text = "Pot: 0";

        string player;
        if (first_to_act == 0)
            player = "Hero";
        else
            player = "Villain";

        log.text += player + " is first to act." + '\n';

        if(first_to_act == 0)
        SetPossibleButtons();
        else
        {
            MakeAllButtonsUnusable();
            DecideActionAI();
        }
    }


    void Start()
    {
        //Setting up ranks
        ranks = new Dictionary<string, int>();
        ranks.Add("KK", 1);
        ranks.Add("QQ", 2);
        ranks.Add("JJ", 3);
        ranks.Add("KQ", 4);
        ranks.Add("QK", 4);
        ranks.Add("KJ", 5);
        ranks.Add("JK", 5);
        ranks.Add("QJ", 6);
        ranks.Add("JQ", 6);

        actions_e = new Dictionary<string, string>();
        actions_e.Add("B", "bets");
        actions_e.Add("c", "checks");
        actions_e.Add("C", "calls");
        actions_e.Add("F", "folds");
        actions_e.Add("R", "raises");

        //Setting up listeners
        bet_button.onClick.AddListener(HandleBet);
        check_button.onClick.AddListener(HandleCheck);
        raise_button.onClick.AddListener(HandleRaise);
        fold_button.onClick.AddListener(HandleFold);
        call_button.onClick.AddListener(HandleCall);
        next_hand_button.onClick.AddListener(HandleNextHand);
        replay_button.onClick.AddListener(ReplayLastHand);
        next_action_button.onClick.AddListener(NextActionReplay);
        previous_action_button.onClick.AddListener(PreviousActionReplay);

        //Loading the AI
        AI = new Dictionary<string, List<float>>();
        BootAI();

        //Hero is first to act
        first_to_act = 0;
        deck = GetDeck();
        InitializeGame();
    }


    //AI PART
    private Dictionary<string, List<float>> AI;
    
    void BootAI()
    {
        string path = "Assets/StrategyLeduc/1k_iterations.csv";

        StreamReader reader = new StreamReader(path);

        string line;
        while((line = reader.ReadLine()) != null)
        {
            string[] processed_string = line.Split(' ');
            string key = processed_string[0];
            List<float> strategy = new List<float>();

            for(int i = 1; i <= 5; i++)
            {
                float probability;
                if (i == 1)
                    probability = float.Parse(processed_string[i].Substring(1, 4));
                else
                    probability = float.Parse(processed_string[i].Substring(0, 4));
                strategy.Add(probability);
            }

            AI.Add(key, strategy);
        }
    }

    void DecideActionAI()
    {
        System.Random random = new System.Random();
        string key = opponent_card_r[0].ToString() + "/";

        if (street == 1)
            key += community_card_r[0].ToString();
        key += "-";

        foreach(char action in history)
        {
            key += action.ToString() + "/";
        }
        if(history != "")
            key = key.Remove(key.Length - 1, 1);

        Debug.Log(key);

        List<float> strategy = new List<float>();
        strategy = AI[key];

        int[] possible_actions = GetPossibleActionsAI();

        //normalize strategy for possible actions
        float sum = 0;
        for(int i = 0; i <= 4; i++)
        {
            if(possible_actions[i] == 1)
            {
                strategy[i] *= 100;
                sum += strategy[i];
            }
        }

        int min = 0;
        int max = (int)sum;
        int choice = random.Next(min, max);

        int counter = 0;
        int picked_action = 5;
        //picking the action
        for(int i = 0; i <= 4; i++)
        {
            if(possible_actions[i] == 1)
            {
                counter += (int)strategy[i];
                if (choice < counter)
                {
                    picked_action = i;
                    break;
                }
            }
        }

        if (picked_action == 0)
            HandleCall();
        if (picked_action == 1)
            HandleBet();
        if (picked_action == 2)
            HandleCheck();
        if (picked_action == 3)
            HandleRaise();
        if (picked_action == 4)
            HandleFold();
        if (picked_action == 5)
            Debug.Log("ERROR");
    }

    //PROBABILITIES ARE ORDERED AS CALL 0 /BET 1 /CHECK 2/RAISE 3/FOLD 4
    int[] GetPossibleActionsAI()
    {
        int[] result = { 0, 0, 0, 0, 0 };
        if (last_action == "none" || last_action == "c")
        {
            result[1] = 1;
            result[2] = 1;
        }

        if (last_action == "B")
        {
            result[3] = 1;
            result[0] = 1;
            result[4] = 1;
        }

        if (last_action == "R" && re_raise_param < 2)
        {
            result[3] = 1;
            result[0] = 1;
            result[4] = 1;
        }

        if (last_action == "R" && re_raise_param == 2)
        {
            result[0] = 1;
            result[4] = 1;
        }
        return result;
    }

    List<float> GetOddsAI(string history)
    {
        List<float> odds = new List<float>();

        string key = opponent_card_r[0].ToString() + "/";
        if (street == 1)
            key += community_card_r[0].ToString();
        key += "-";

        foreach (char action in history)
        {
            key += action.ToString() + "/";
        }
        if (history != "")
            key = key.Remove(key.Length - 1, 1);
        Debug.Log(key);
        odds = AI[key];

        return odds;
    }
}
