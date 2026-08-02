#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

public partial class ContainerInventory : ItemStorage
{
	// Zero remains useful for large authored caches and backwards-compatible
	// prototype containers. Physical containers can opt into believable limits.
	[Export(PropertyHint.Range, "0,128,1,or_greater")]
	public int SlotCapacity { get; set; }

	public override int Capacity => SlotCapacity;

	// Version-1 containers were initially unbounded. Restoring all of an older
	// container's stacks is safer than rejecting the complete save or discarding
	// supplies. New additions remain capacity-limited, and taking items resolves
	// the overflow deterministically from the visible stack list.
	protected override bool AllowRestorePastCapacity => true;
}
