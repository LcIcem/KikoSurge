/// <summary>
/// 运行时 item 实例（存在于背包中）
/// </summary>
public class ItemInstance
{
    public ItemConfig Definition { get; }
    public int Quantity { get; set; }

    public ItemInstance(ItemConfig definition, int quantity)
    {
        Definition = definition;
        Quantity = quantity;
    }
}
