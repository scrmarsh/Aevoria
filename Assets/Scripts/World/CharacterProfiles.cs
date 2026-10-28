using System;
namespace Aevoria.World
{
    public enum CharacterAppearance { Explorer,Roman,Doctor }
    public static class CharacterProfiles
    {
        public const string Explorer="explorer",Roman="roman",Doctor="doctor";
        public static CharacterAppearance Appearance(string id)=>id==Roman?CharacterAppearance.Roman:id==Doctor?CharacterAppearance.Doctor:CharacterAppearance.Explorer;
        public static bool Valid(string id)=>id==Explorer||id==Roman||id==Doctor;
        public static bool UnlocksDoctor(string phrase)=>string.Equals(phrase?.Trim(),"EVERYBODY LIES",StringComparison.OrdinalIgnoreCase);
        public static readonly string[] DoctorDialogue={
            "Everybody lies.",
            "It's not lupus.",
            "Humanity is overrated.",
            "Reality is almost always wrong.",
            "Never trust doctors.",
            "Weird works for me.",
            "Truth begins in lies.",
            "Everybody dies alone.",
            "People rarely change who they really are.",
            "Being nice doesn't automatically make you right.",
            "Just because you're offended doesn't mean you're correct.",
            "Most people prefer comforting lies to uncomfortable truths.",
            "Stupidity is practically a universal condition.",
            "Being miserable doesn't make you special.",
            "Everyone has an excuse. Very few have a good one.",
            "You don't need to like someone to save their life.",
            "The truth doesn't care how you feel about it.",
            "Sometimes doing the right thing makes everyone hate you.",
            "People tend to believe whatever makes them happiest.",
            "If the obvious answer were correct, you wouldn't need me.",
            "You can be completely sincere and completely wrong.",
            "Common sense isn't nearly as common as people think.",
            "People only want your opinion when it agrees with theirs.",
            "An apology doesn't magically fix what happened.",
            "If everyone agrees with you, you're probably missing something.",
            "You can't solve a problem by pretending it doesn't exist.",
            "Being confident and being correct are two very different things.",
            "Sometimes the smartest thing you can say is nothing.",
            "People don't always get what they deserve.",
            "The world doesn't owe you a happy ending."
        };
    }
}
