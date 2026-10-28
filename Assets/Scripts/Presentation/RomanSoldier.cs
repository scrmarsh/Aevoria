using Aevoria.World;
namespace Aevoria.Presentation
{
    // Compatibility adapter for the preserved Roman appearance and its tests.
    public sealed class RomanSoldier:HumanoidPresentation
    {
        public new void Initialize()=>base.Initialize(CharacterAppearance.Roman);
    }
}
