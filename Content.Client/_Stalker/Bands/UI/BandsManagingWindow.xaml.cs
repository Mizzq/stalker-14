using Content.Client.UserInterface.Controls;
using Content.Shared._Stalker.Bands;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._Stalker.Bands.UI;

[GenerateTypedNameReferences]
public sealed partial class BandsManagingWindow : FancyWindow
{
    public event Action<string>? OnAddMemberButtonPressed;
    public event Action<Guid>? OnRemoveMemberButtonPressed;
    public event Action<string>? OnBuyItemButtonPressed; // Event for buying items

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!; // For entity prototype checks

    public BandsManagingWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        Tabs.SetTabTitle(0, Loc.GetString("bands-managing-window-members-tab-title"));
        Tabs.SetTabTitle(1, Loc.GetString("bands-managing-window-warzone-tab-title"));
        Tabs.SetTabTitle(2, Loc.GetString("bands-managing-window-shop-tab-title")); // Set title for the new shop tab

        AddMemberButton.OnPressed += OnAddMemberPressed;
    }

    private void OnAddMemberPressed(BaseButton.ButtonEventArgs args)
    {
        OnAddMemberButtonPressed?.Invoke(AddMemberLineEdit.Text);
        AddMemberLineEdit.Text = string.Empty;
    }

    public void UpdateState(BandsManagingBoundUserInterfaceState state)
    {
        BandNameLabel.Text = state.BandName ?? Loc.GetString("bands-managing-window-band-name-unknown");
        BandMemberCountLabel.Text = $"{state.Members.Count} / {state.MaxMembers}";

        // --- Clear Lists ---
        MembersList.Children.Clear();
        WarZoneList.Children.Clear();
        BandPointsList.Children.Clear();
        ShopItemList.Children.Clear(); // Clear shop items list

        // --- Update Members Tab ---
        var canManage = state.CanManage;
        if (state.Members.Count == 0)
        {
            MembersList.Children.Add(NoMembersLabel);
            NoMembersLabel.Visible = true;
        }
        else
        {
            NoMembersLabel.Visible = false;
            foreach (var member in state.Members)
            {
                var memberBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 2)
                };

                var nameLabel = new Label
                {
                    Text = member.CharacterName, // Display CharacterName instead of PlayerName (ckey)
                    HorizontalExpand = true,
                };

                var removeButton = new Button
                {
                    Text = Loc.GetString("bands-managing-window-remove-button"),
                    MinWidth = 80,
                    HorizontalAlignment = HAlignment.Right,
                    Disabled = !canManage
                };

                var memberId = member.UserId;
                removeButton.OnPressed += _ => OnRemoveMemberButtonPressed?.Invoke(memberId);

                memberBox.AddChild(nameLabel);
                memberBox.AddChild(removeButton);

                MembersList.AddChild(memberBox);
            }
        }
        AddMemberLineEdit.Editable = canManage;
        AddMemberButton.Disabled = !canManage;

        // --- Update War Zone Tab ---

        // Populate War Zones List
        if (state.WarZones.Count == 0)
        {
            WarZoneList.Children.Add(NoWarZonesLabel);
            NoWarZonesLabel.Visible = true;
        }
        else
        {
            NoWarZonesLabel.Visible = false;
            foreach (var wz in state.WarZones)
            {
                var wzBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 4)
                };

                wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-zone", ("zoneId", wz.ZoneId)) });
                wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-owner", ("owner", wz.Owner)), StyleClasses = { "LabelSecondaryColor" } });
                wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-cooldown", ("seconds", $"{wz.Cooldown:F0}")), StyleClasses = { "LabelSecondaryColor" } });
                wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-attacker", ("attacker", wz.Attacker)), StyleClasses = { "LabelSecondaryColor" } });
                wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-defender", ("defender", wz.Defender)), StyleClasses = { "LabelSecondaryColor" } });
                // wzBox.AddChild(new Label { Text = Loc.GetString("bands-managing-window-warzone-progress", ("progress", $"{wz.Progress * 100:F1}")), StyleClasses = { "LabelSecondaryColor" } });

                WarZoneList.AddChild(wzBox);
                // Add a separator for readability, except after the last item
                if (state.WarZones.IndexOf(wz) < state.WarZones.Count - 1)
                {
                    WarZoneList.AddChild(new PanelContainer { StyleClasses = { "LowDivider" } });
                }
            }
        }

        // Populate Band Points List
        if (state.BandPoints.Count == 0)
        {
            BandPointsList.Children.Add(NoBandPointsLabel);
            NoBandPointsLabel.Visible = true;
        }
        else
        {
            NoBandPointsLabel.Visible = false;
            foreach (var bp in state.BandPoints)
            {
                var bpBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 2)
                };

                var nameLabel = new Label
                {
                    Text = Loc.GetString("bands-managing-window-band-points-entry", ("bandName", bp.BandName), ("points", bp.Points)),
                    HorizontalExpand = true,
                };

                bpBox.AddChild(nameLabel);
                BandPointsList.AddChild(bpBox);
            }
        }

        // --- Update Shop Tab ---
        if (state.ShopItems.Count == 0)
        {
            ShopItemList.Children.Add(NoShopItemsLabel);
            NoShopItemsLabel.Visible = true;
        }
        else
        {
            NoShopItemsLabel.Visible = false;
            foreach (var item in state.ShopItems)
            {
                // Try to get the entity prototype for the name and icon
                string itemName = item.ProductEntity; // Default to ID
                Texture? itemTexture = null;
                if (_prototypeManager.TryIndex<EntityPrototype>(item.ProductEntity, out var itemProto))
                {
                    itemName = itemProto.Name; // Use prototype name if available
                    itemTexture = _entityManager.System<SpriteSystem>().GetPrototypeIcon(itemProto).Default;
                }


                var itemBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    Margin = new Thickness(0, 4),
                    SeparationOverride = 5
                };

                // Icon
                if (itemTexture != null)
                {
                    itemBox.AddChild(new TextureRect
                    {
                        Texture = itemTexture,
                        MinSize = new System.Numerics.Vector2(32, 32), // Corrected to use Vector2 constructor
                        Stretch = TextureRect.StretchMode.KeepAspectCentered
                    });
                }

                // Name and Price Label
                var namePriceLabel = new Label
                {
                    Text = Loc.GetString("bands-managing-window-shop-item-entry", ("itemName", itemName), ("price", item.Price)),
                    HorizontalExpand = true,
                    VerticalAlignment = VAlignment.Center
                };

                // Buy Button
                var buyButton = new Button
                {
                    Text = Loc.GetString("bands-managing-window-buy-button"), // Use "Buy" for the band shop
                    MinWidth = 80,
                    HorizontalAlignment = HAlignment.Right,
                    Disabled = !state.CanManage // Only allow managing members to buy for the band
                };

                var itemId = item.ProductEntity; // Capture item ID for the button action
                buyButton.OnPressed += _ => OnBuyItemButtonPressed?.Invoke(itemId);

                itemBox.AddChild(namePriceLabel);
                itemBox.AddChild(buyButton);

                ShopItemList.AddChild(itemBox);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            AddMemberButton.OnPressed -= OnAddMemberPressed;
        }
    }
}
