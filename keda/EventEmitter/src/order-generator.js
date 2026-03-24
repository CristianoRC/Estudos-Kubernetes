import { randomUUID } from "crypto";

const PRODUCTS = [
  { id: "PROD-001", name: "Notebook Dell XPS", price: 4500 },
  { id: "PROD-002", name: "Monitor Samsung 27", price: 1800 },
  { id: "PROD-003", name: "Teclado Mecânico Keychron", price: 750 },
  { id: "PROD-004", name: "Mouse Logitech MX Master", price: 550 },
  { id: "PROD-005", name: "Headset Sony WH-1000XM5", price: 2200 },
];

export function generateOrder(index) {
  const product = PRODUCTS[(index - 1) % PRODUCTS.length];
  const quantity = (index % 3) + 1;

  return {
    OrderId: `ORD-${randomUUID().replace(/-/g, "").substring(0, 12).toUpperCase()}`,
    CustomerId: `CUST-${String((index % 10) + 1).padStart(4, "0")}`,
    CustomerName: `Customer ${(index % 10) + 1}`,
    TotalAmount: product.price * quantity,
    Currency: "BRL",
    Items: [
      {
        ProductId: product.id,
        ProductName: product.name,
        Quantity: quantity,
        UnitPrice: product.price,
      },
    ],
    CreatedAt: new Date().toISOString(),
  };
}
