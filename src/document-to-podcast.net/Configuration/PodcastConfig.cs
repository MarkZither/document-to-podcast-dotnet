using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DocumentToPodcast.Configuration;

public class PodcastConfig
{
    [Required]
    [JsonPropertyName("InputFile")]
    public string InputFile { get; set; } = string.Empty;
    
    [Required]
    [JsonPropertyName("OutputFolder")]
    public string OutputFolder { get; set; } = string.Empty;
    
    [JsonPropertyName("TextToTextModel")]
    public string TextToTextModel { get; set; } = "http://localhost:11434";
    
    [JsonPropertyName("TextToTextPrompt")]
    public string TextToTextPrompt { get; set; } = DefaultPrompt;
    
    [JsonPropertyName("TextToSpeechModel")]
    public string TextToSpeechModel { get; set; } = "http://localhost:11434";
    
    [JsonPropertyName("Speakers")]
    public List<Speaker> Speakers { get; set; } = DefaultSpeakers;
    
    [JsonPropertyName("Language")]
    public string Language { get; set; } = "en";

    public static readonly string DefaultPrompt = """
        You are a podcast scriptwriter generating engaging and natural-sounding conversations in JSON format.
        The script features the following speakers:
        {SPEAKERS}
        Instructions:
        - Write dynamic, easy-to-follow dialogue.
        - Include natural interruptions and interjections.
        - Avoid repetitive phrasing between speakers.
        - Format output as a JSON conversation.
        Example:
        {
          "Speaker 1": "Welcome to our podcast! Today, we're exploring...",
          "Speaker 2": "Hi! I'm excited to hear about this. Can you explain...",
          "Speaker 1": "Sure! Imagine it like this...",
          "Speaker 2": "Oh, that's cool! But how does..."
        }
        """;

    public static readonly List<Speaker> DefaultSpeakers = new()
    {
        new Speaker
        {
            Id = 1,
            Name = "Laura",
            Description = "The main host. She explains topics clearly using anecdotes and analogies, teaching in an engaging and captivating way.",
            VoiceProfile = "female_1"
        },
        new Speaker
        {
            Id = 2,
            Name = "Jon", 
            Description = "The co-host. He keeps the conversation on track, asks curious follow-up questions, and reacts with excitement or confusion, often using interjections like hmm or umm.",
            VoiceProfile = "male_1"
        }
    };
}

public class Speaker
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }
    
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("Description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("VoiceProfile")]
    public string VoiceProfile { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Speaker {Id}. Named {Name}. {Description}";
    }
}
