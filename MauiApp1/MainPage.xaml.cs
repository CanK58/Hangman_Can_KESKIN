using Plugin.Maui.Audio;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
    private static readonly string[] Words =
    [
        "FREIN", "JANTE", "HUILE", "PHARE", "ROUES", "RADIO", "TURBO",
        "PNEUS", "PANNE", "POMPE", "PISTE", "CARTE", "ROUTE"
    ];

    private readonly Dictionary<string, IAudioPlayer> soundPlayers = [];
    private readonly List<Button> guessedButtons = [];
    private readonly HashSet<char> correctGuesses = [];
    private Label[] letterLabels = [];
    private Label[] tires = [];
    private Label[] tireFires = [];
    private Label[] tireSmoke = [];
    private string word = "FREIN";
    private int errors;
    private bool gameOver;
    private bool isProcessingGuess;

    public MainPage()
    {
        InitializeComponent();
        letterLabels = [Letter1, Letter2, Letter3, Letter4, Letter5];
        tires = [Tire1, Tire2, Tire3, Tire4, Tire5];
        tireFires = [TireFire1, TireFire2, TireFire3, TireFire4, TireFire5];
        tireSmoke = [TireSmoke1, TireSmoke2, TireSmoke3, TireSmoke4, TireSmoke5];
    }

    private async void Button_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
            throw new InvalidOperationException("The guess event must come from a button.");

        await GuessLetterAsync(button);
    }

    private async Task GuessLetterAsync(Button button)
    {
        if (gameOver || isProcessingGuess)
            return;

        if (!button.IsEnabled)
            return;

        isProcessingGuess = true;
        try
        {
            button.IsEnabled = false;
            guessedButtons.Add(button);
            await Task.WhenAll(
                button.ScaleTo(0.9, 65),
                PlaySoundAsync("key.wav"));
            await button.ScaleTo(1, 95);

            string guess = button.Text;
            if (word.Contains(guess, StringComparison.Ordinal))
            {
                button.Background = new SolidColorBrush(Color.FromArgb("#218C66"));
                correctGuesses.Add(guess[0]);

                for (int i = 0; i < word.Length; i++)
                {
                    if (word[i].ToString() != guess)
                        continue;

                    Label letter = letterLabels[i];
                    letter.Text = guess;
                    await Task.WhenAll(
                        letter.ScaleTo(1.2, 100),
                        letter.FadeTo(0.7, 80));
                    await Task.WhenAll(
                        letter.ScaleTo(1, 100),
                        letter.FadeTo(1, 100));
                }

                if (word.All(correctGuesses.Contains))
                {
                    gameOver = true;
                    await PlaySoundAsync("win.wav");
                    await ShowOverlayAsync(WinOverlay, WinCard);
                }
            }
            else
            {
                button.Background = new SolidColorBrush(Color.FromArgb("#4A555D"));
                ShowNextHangmanPart();
                LifeStatus.Text = $"{tires.Length - errors} / {tires.Length}";
                await Task.WhenAll(
                    AnimateHangmanPartAsync(GetCurrentHangmanPart()),
                    BurnTireAsync(errors - 1),
                    ShakeKeypadAsync());

                if (errors == tires.Length)
                {
                    gameOver = true;
                    await PlaySoundAsync("lose.wav");
                    await ShowOverlayAsync(GameOverOverlay, GameOverCard);
                }
            }
        }
        finally
        {
            isProcessingGuess = false;
            KeyboardInput.Focus();
        }
    }

    private void KeyboardInput_Loaded(object? sender, EventArgs e)
    {
        if (sender is Entry entry)
            entry.Focus();
    }

    private async void KeyboardInput_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string text = entry.Text?.Trim().ToUpperInvariant() ?? string.Empty;
        entry.Text = string.Empty;

        if (text.Length == 0)
            return;

        char letter = text[^1];
        if (letter is < 'A' or > 'Z')
            return;

        Button? button = KeypadPanel.Children
            .OfType<Button>()
            .FirstOrDefault(candidate => candidate.Text == letter.ToString());

        if (button is not null)
            await GuessLetterAsync(button);
    }

    private async void ReplayButton_Clicked(object? sender, EventArgs e)
    {
        await GameOverOverlay.FadeTo(0, 140);
        ResetGame();
    }

    private async void NextWordButton_Clicked(object? sender, EventArgs e)
    {
        await PlaySoundAsync("key.wav");
        string[] possibleWords = Words.Where(candidate => candidate != word).ToArray();
        word = possibleWords[Random.Shared.Next(possibleWords.Length)];
        await WinOverlay.FadeTo(0, 140);
        ResetGame();
    }

    private void ShowNextHangmanPart()
    {
        errors++;

        HangmanHead.IsVisible = errors >= 1;
        HangmanBody.IsVisible = errors >= 2;
        HangmanLeftArm.IsVisible = errors >= 3;
        HangmanRightArm.IsVisible = errors >= 4;
        HangmanLegs.IsVisible = errors >= 5;
    }

    private View GetCurrentHangmanPart() => errors switch
    {
        1 => HangmanHead,
        2 => HangmanBody,
        3 => HangmanLeftArm,
        4 => HangmanRightArm,
        5 => HangmanLegs,
        _ => throw new InvalidOperationException("No hangman part corresponds to the current error count.")
    };

    private async Task AnimateHangmanPartAsync(View part)
    {
        part.Opacity = 0;
        part.Scale = 0.6;
        await Task.WhenAll(
            part.FadeTo(1, 180),
            part.ScaleTo(1, 240, Easing.BounceOut));
    }

    private async Task BurnTireAsync(int index)
    {
        Label tire = tires[index];
        Label fire = tireFires[index];
        Label smoke = tireSmoke[index];

        await Task.WhenAll(
            tire.RotateTo(300, 240, Easing.CubicIn),
            tire.FadeTo(0, 220));
        tire.IsVisible = false;

        fire.IsVisible = true;
        smoke.IsVisible = true;
        fire.Opacity = 0;
        fire.Scale = 0.5;
        smoke.Opacity = 0;
        smoke.TranslationY = 8;

        await Task.WhenAll(
            fire.FadeTo(1, 150),
            fire.ScaleTo(1.15, 220, Easing.BounceOut),
            smoke.FadeTo(1, 180),
            smoke.TranslateTo(0, -7, 300, Easing.CubicOut));
    }

    private async Task ShakeKeypadAsync()
    {
        await KeypadPanel.TranslateTo(-5, 0, 45);
        await KeypadPanel.TranslateTo(5, 0, 45);
        await KeypadPanel.TranslateTo(0, 0, 45);
    }

    private async Task ShowOverlayAsync(Grid overlay, Border card)
    {
        overlay.Opacity = 1;
        card.Opacity = 0;
        card.Scale = 0.78;
        overlay.IsVisible = true;

        await Task.WhenAll(
            card.FadeTo(1, 180),
            card.ScaleTo(1, 300, Easing.SpringOut));
    }

    private async Task PlaySoundAsync(string fileName)
    {
        if (!soundPlayers.TryGetValue(fileName, out IAudioPlayer? player))
        {
            Stream audioStream = await FileSystem.OpenAppPackageFileAsync(fileName);
            player = AudioManager.Current.CreatePlayer(audioStream);
            player.Volume = 0.55;
            soundPlayers.Add(fileName, player);
        }

        player.Stop();
        player.Play();
    }

    private void ResetGame()
    {
        errors = 0;
        gameOver = false;
        isProcessingGuess = false;
        GameOverOverlay.IsVisible = false;
        WinOverlay.IsVisible = false;
        correctGuesses.Clear();
        LifeStatus.Text = $"{tires.Length} / {tires.Length}";

        foreach (Label letter in letterLabels)
        {
            letter.Text = "_";
            letter.Scale = 1;
            letter.Opacity = 1;
        }

        HangmanHead.IsVisible = false;
        HangmanBody.IsVisible = false;
        HangmanLeftArm.IsVisible = false;
        HangmanRightArm.IsVisible = false;
        HangmanLegs.IsVisible = false;

        for (int i = 0; i < tires.Length; i++)
        {
            tires[i].IsVisible = true;
            tires[i].Opacity = 1;
            tires[i].Rotation = 0;
            tireFires[i].IsVisible = false;
            tireFires[i].Opacity = 1;
            tireFires[i].Scale = 1;
            tireSmoke[i].IsVisible = false;
            tireSmoke[i].Opacity = 1;
            tireSmoke[i].TranslationY = 0;
        }

        foreach (Button button in guessedButtons)
        {
            button.IsEnabled = true;
            button.Background = new SolidColorBrush(Color.FromArgb("#C83D3D"));
            button.Scale = 1;
        }

        guessedButtons.Clear();
        KeyboardInput.Focus();
    }

    protected override void OnDisappearing()
    {
        foreach (IAudioPlayer player in soundPlayers.Values)
            player.Dispose();

        soundPlayers.Clear();
        base.OnDisappearing();
    }
}
