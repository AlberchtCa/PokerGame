using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RhodeGameHandler : MonoBehaviour
{
    public GameObject hero_card;
    public GameObject opponent_card;
    public GameObject community_card1;
    public GameObject community_card2;
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
    public string community_card_r1;
    public string community_card_r2;

    private List<int> deck_indexes;
    private List<string> deck;
    private int first_to_act;
    private string history;
    private int re_raise_param;
    private int street;
    private string last_action;
    private char[] actions = { 'C', 'B', 'c', 'R', 'F' };
    private int acting_player;
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

    //for AI
    private Dictionary<string, List<float>> AI;

    private void Start()
    {
        //Setting up the log for actions.
        actions_e = new Dictionary<string, string>();
        actions_e.Add("B", "bets");
        actions_e.Add("c", "checks");
        actions_e.Add("C", "calls");
        actions_e.Add("R", "raises");
        actions_e.Add("F", "folds");

        //Setting up listeners

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

        //Setting up the AI
        AI = new Dictionary<string, List<float>>();
        BootAI();

        //Hero is first to act
        first_to_act = 0;
        deck = GetDeck();
        InitializeGame();
    }

    //Game functions

    private void InitializeGame()
    {
        //Enable action buttons
        ModifyActionButtons(true);

        //Disable simulation/end-game buttons
        replay_button_holder.SetActive(false);
        previous_action_button_holder.SetActive(false);
        next_action_button_holder.SetActive(false);
        is_simulation = false;
        strategy_holder.SetActive(false);
        next_hand_button_holder.SetActive(false);

        //Set community and opponent cards face down
        community_card1.GetComponent<SpriteRenderer>().sprite = card_sprites[20];
        community_card2.GetComponent<SpriteRenderer>().sprite = card_sprites[20];
        opponent_card.GetComponent<SpriteRenderer>().sprite = card_sprites[20];

        //Get the four cards that will be in play
        deck_indexes = GetCardIndexes();

        //Associate indexes with cards
        hero_card_r = deck[deck_indexes[0]];
        opponent_card_r = deck[deck_indexes[1]];
        community_card_r1 = deck[deck_indexes[2]];
        community_card_r2 = deck[deck_indexes[3]];

        hero_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[0]];
        history = "";
        log.text = "";
        acting_player = first_to_act;
        street = 0;
        last_action = "none";
        re_raise_param = 0;
        pot.text = "Pot: 0";

        string player;
        if (first_to_act == 0)
            player = "Hero";
        else
            player = "Villain";
        log.text += player + " is first to act." + '\n';

        if (first_to_act == 0)
            SetPossibleButtons();
        else
        {
            MakeAllButtonsUnusable();
            DecideActionAI();
        }

    }

    private void MakeAllButtonsUnusable()
    {
        fold_button.interactable = false;
        check_button.interactable = false;
        raise_button.interactable = false;
        call_button.interactable = false;
        bet_button.interactable = false;
        call_button_holder.SetActive(false);
        check_button_holder.SetActive(false);
    }

    private void SetPossibleButtons()
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

    private List<int> GetCardIndexes()
    {
        List<int> deck_indexes = new List<int>();
        System.Random random = new System.Random();
        int i = 0;
        int curr_number = 0;
        while (i != 4)
        {
            curr_number = random.Next(0, 20);
            if (!deck_indexes.Contains(curr_number))
            {
                deck_indexes.Add(curr_number);
                i++;
            }
        }
        return deck_indexes;
    }

    private void ModifyActionButtons(bool activity)
    {
        fold_button_holder.SetActive(activity);
        check_button_holder.SetActive(activity);
        raise_button_holder.SetActive(activity);
        call_button_holder.SetActive(activity);
        bet_button_holder.SetActive(activity);
        call_button_holder.SetActive(activity);
        check_button_holder.SetActive(activity);
    }

    private List<string> GetDeck()
    {
        List<string> deck = new List<string>();
        //T = 0, J = 1, Q = 2, K = 3, A = 4
        //The suits are clubs (c), diamonds (d), hearts (h) and spades (s)
        string[] cards = { "T", "J", "Q", "K", "A" };
        string[] suits = { "c", "d", "h", "s" };
        foreach (string card in cards)
            foreach (string suit in suits)
            {
                deck.Add(card + suit);
            }

        return deck;
    }

    //AI Functions

    private void DecideActionAI()
    {
        System.Random random = new System.Random();
        string key = opponent_card_r[0].ToString() + "/";

        if (street == 1)
            key += community_card_r1[0].ToString();
        if (street == 2)
            key += community_card_r1[0].ToString() + community_card_r2[0].ToString();
        key += "/";

        if (street == 0)
            key += "n";
        if (street == 1)
            if (community_card_r1[1] == opponent_card_r[1])
                key += "d";
            else
                key += "n";
        if (street == 2)
            if (community_card_r1[1] == community_card_r2[1] &&
                community_card_r2[1] == opponent_card_r[1])
                key += "y";
            else
                key += "n";

        key += "-";

        foreach (char action in history)
        {
            key += action.ToString() + "/";
        }

        if (history != "")
            key = key.Remove(key.Length - 1, 1);

        Debug.Log(key);

        List<float> strategy = new List<float>();
        strategy = AI[key];

        int[] possible_actions = GetPossibleActionsAI();

        float sum = 0;
        for (int i = 0; i <= 4; i++)
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

        for (int i = 0; i <= 4; i++)
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
        /*
        int choice = random.Next(0, 5);

        int[] possible_actions = GetPossibleActionsAI();

        while (possible_actions[choice] != 1)
            choice = random.Next(0, 5);

        int picked_action = choice;
        */
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

    private int[] GetPossibleActionsAI()
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

    private void BootAI()
    {
        string path = "Assets/StrategyRhode/10k_iterations.csv";
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
    
    private List<float> GetOddsAI(string history)
    {
        List<float> odds = new List<float>();

        string key = opponent_card_r[0].ToString() + "/";
        if (street == 1)
            key += community_card_r1[0].ToString();
        if (street == 2)
            key += community_card_r1[0].ToString() + community_card_r2[0].ToString();
        key += "/";

        if (street == 0)
            key += "n";
        if (street == 1)
            if (community_card_r1[1] == opponent_card_r[1])
                key += "d";
            else
                key += "n";
        if (street == 2)
            if (community_card_r1[1] == community_card_r2[1] &&
                community_card_r2[1] == opponent_card_r[1])
                key += "y";
            else
                key += "n";

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

    //Button handler functions

    private void PreviousActionReplay()
    {
        if (action_replay_index != 0)
        {
            action_replay_index -= 1;
            SimulateHand(history.Substring(0, action_replay_index));

        }
    }

    private void NextActionReplay()
    {
        if (action_replay_index < history.Length)
        {
            action_replay_index += 1;
            SimulateHand(history.Substring(0, action_replay_index));
        }
    }

    private void ReplayLastHand()
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


    private void HandleNextHand()
    {
        InitializeGame();
    }

    private void HandleCall()
    {
        if (street == 2)
            HandleActionHelper("C", true, false, true);
        else
            HandleActionHelper("C", true, true, false);
    }

    private void HandleFold()
    {
        HandleActionHelper("F", false, false, true);
    }

    private void HandleRaise()
    {
        re_raise_param += 1;
        HandleActionHelper("R", true, false, false);
    }

    private void HandleCheck()
    {
        if (last_action == "none")
        {
            HandleActionHelper("c", false, false, false);
        }
        else
        {
            if (street == 2)
                HandleActionHelper("c", false, false, true);
            else
                HandleActionHelper("c", false, true, false);
        }
    }

    private void HandleBet()
    {
        HandleActionHelper("B", true, false, false);
    }

    private void HandleActionHelper(string action, bool pot_updated, bool new_street, bool is_game_over)
    {
        if (is_simulation)
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
            if(street == 1 || street == 2)
                in_pot = (Int32.Parse(in_pot) - 4).ToString();
            else
                in_pot = (Int32.Parse(in_pot) - 2).ToString();
            pot.text = "Pot: " + in_pot;
        }

        if (new_street)
        {
            last_action = "none";
            street += 1;
            re_raise_param = 0;
            if(street == 1)
            {
                community_card1.SetActive(true);
                community_card1.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[2]];
            }
            if(street == 2)
            {
                community_card2.SetActive(true);
                community_card2.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[3]];
            }
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
            if (street == 1)
                community_card1.SetActive(true);
            if (street == 2)
                community_card2.SetActive(true);
        }
    }

    private void GameOver()
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
            community_card1.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[2]];
            community_card2.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[3]];
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

    private int DetermineWinner()
    {
        List<string> hero_hand = new List<string>();
        List<string> opp_hand = new List<string>();

        hero_hand.Add(hero_card_r);
        hero_hand.Add(community_card_r1);
        hero_hand.Add(community_card_r2);

        opp_hand.Add(opponent_card_r);
        opp_hand.Add(community_card_r1);
        opp_hand.Add(community_card_r2);

        int hero_rank = GetRank(hero_hand);
        int opp_rank = GetRank(opp_hand);

        Debug.Log(hero_rank);
        Debug.Log(opp_rank);

        if (hero_rank < opp_rank)
            return 0;
        if (opp_rank < hero_rank)
            return 1;
        if (hero_rank == opp_rank)
        {
            List<int> count1 = new List<int>();
            List<int> count2 = new List<int>();

            count1 = CountCards(hero_hand);
            count2 = CountCards(opp_hand);

            for(int i = 4; i >= 0; i--)
            {
                if (count1[i] > count2[i])
                    return 0;
                if (count1[i] < count2[i])
                    return 1;
            }
        }
        return 2;
    }

    private List<int> CountCards(List<string> hand)
    {
        List<char> cards = new List<char>();
        List<int> count = new List<int>();

        cards.Add('T');
        cards.Add('J');
        cards.Add('Q');
        cards.Add('K');
        cards.Add('A');

        for(int i = 0; i < 5; i++)
        {
            count.Add(0);
        }
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 5; j++)
                if (hand[i][0] == cards[j])
                    count[j] += 1;

        return count;
    }

    private int GetRank(List<string> hand)
    {
        List<int> count = new List<int>();
        count = CountCards(hand);

        if (IsStraightFlush(hand, count))
            return 1;
        if (IsTriplet(count))
            return 2;
        if (IsStraight(count))
            return 3;
        if (IsFlush(hand))
            return 4;
        if (IsPair(count))
            return 5;

        return 6;
    }

    private bool IsPair(List<int> count)
    {
        if (count.Contains(2))
            return true;
        return false;
    }

    private bool IsFlush(List<string> hand)
    {
        for (int i = 0; i < 2; i++)
            if (hand[i][1] != hand[i + 1][1])
                return false;
        return true;
    }

    private bool IsStraight(List<int> count)
    {
        for (int i = 0; i < 3; i++)
            if (count[i] == count[i + 1] && count[i + 1] == count[i + 2])
                if(count[i] == 1)
                    return true;
        return false;
    }

    private bool IsTriplet(List<int> count)
    {
        if (count.Contains(3))
            return true;
        return false;
    }

    private bool IsStraightFlush(List<string> hand, List<int> count)
    {
        if (IsFlush(hand) && IsStraight(count))
            return true;
        return false;
    }

    private void UpdatePot()
    {
        string in_pot = pot.text;
        in_pot = in_pot.Substring(in_pot.IndexOf(": ") + 2);
        if(street == 0 || street == 1)
            in_pot = (Int32.Parse(in_pot) + 2 + 2 * street).ToString();
        else
            in_pot = (Int32.Parse(in_pot) + 4).ToString();
        pot.text = "Pot: " + in_pot;
    }



    //Replay Functions

    private void InitializeSimulation()
    {
        simulation_history = "";
        pot.text = "Pot: 0";
        opponent_card.GetComponent<SpriteRenderer>().sprite = card_sprites[deck_indexes[1]];
        community_card1.SetActive(false);
        community_card2.SetActive(false);
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

    private void SimulateHand(string current_history)
    {
        InitializeSimulation();
        foreach (char action in current_history)
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

    private void ModifyHistoryButtons(bool activity)
    {
        next_action_button_holder.SetActive(activity);
        previous_action_button_holder.SetActive(activity);
        strategy_holder.SetActive(activity);
    }
}
