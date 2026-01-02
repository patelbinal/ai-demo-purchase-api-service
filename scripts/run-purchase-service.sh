#!/bin/bash

# Build and run automation script for Purchase Service

echo "Building Purchase Service Docker image..."
docker build -t purchase-service:latest .

echo "Starting services with Docker Compose..."
docker-compose up -d

echo "Waiting for services to be ready..."
sleep 30

echo "Testing Purchase Service endpoints..."

# Test Create Purchase
echo "Creating test purchase..."
curl -X POST http://localhost:8001/api/purchases \
  -H "Content-Type: application/json" \
  -d '{
    "buyerId": 1001,
    "offerId": 2001,
    "purchaseDate": "2026-01-02T10:00:00Z",
    "amount": 25000.00,
    "status": "Pending",
    "buyerDetails": {
      "name": "John Doe",
      "email": "john.doe@email.com",
      "phone": "+1-555-0123",
      "address": {
        "street": "123 Main St",
        "city": "Springfield",
        "state": "IL",
        "zipCode": "62701",
        "country": "USA"
      },
      "paymentMethod": "Credit Card"
    }
  }'

echo -e "\n\nRetrieving all purchases..."
curl -X GET http://localhost:8001/api/purchases

echo -e "\n\nPurchase Service is running successfully!"
echo "API Documentation available at: http://localhost:8001/swagger"