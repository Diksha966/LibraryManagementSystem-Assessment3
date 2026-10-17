# User Journey Maps (Person 3 features)

Mermaid diagrams: paste into any Mermaid renderer (or GitHub) to export images for the portfolio.

## Journey 1: Patron borrows at the kiosk

```mermaid
journey
    title Borrowing an item at the self-service kiosk
    section Arrive
      See the welcome screen: 5: Patron
      Touch "Borrow an item": 5: Patron
    section Identify
      Enter member number and email: 3: Patron
      Details not matched (retry): 2: Patron
    section Borrow
      Scan the library code: 5: Patron
      See confirmation and due date: 5: Patron
    section Finish
      Touch "Finish and sign out": 5: Patron
```

```mermaid
flowchart TD
    A[Kiosk home] --> B[Borrow an item]
    B --> C{Signed in?}
    C -- No --> D[Enter member number + email]
    D --> E{Details match?}
    E -- No --> D
    E -- 5 failures --> X[Ask staff for help]
    E -- Yes --> F
    C -- Yes --> F[Scan or type library code]
    F --> G{Item found and Available?}
    G -- No --> H[Message: not found / not available / held for someone else]
    H --> F
    G -- Yes --> I[Loan created, item set to Borrowed]
    I --> J[Show due date]
    J --> K[Borrow another or Finish]
```

## Journey 2: Reservation and waitlist

```mermaid
sequenceDiagram
    actor P as Patron (Jane)
    participant W as Public item page
    participant R as ReservationService
    actor S as Reception
    actor P1 as Patron (John, current borrower)

    P->>W: Search, open Borrowed item
    W-->>P: "Reserve this item"
    P->>R: Member number + email
    R-->>P: You are number 1 in the queue
    P1->>S: Returns the item
    S->>R: Return recorded, item Available
    R-->>P: Notification: item is ready (logged)
    P->>R: Borrows at kiosk or desk
    R-->>R: Reservation marked Fulfilled
```

## Journey 3: Admin imports items / Manager exports reports

```mermaid
flowchart LR
    A[Admin: Import Items] --> B[Download template]
    B --> C[Upload CSV]
    C --> D[Preview: valid rows and errors]
    D -->|Import valid rows| E[Items added as Available]
    D -->|Cancel| A
    M[Manager: Reports] --> N[Preview report]
    N --> O[Download CSV]
    N --> P[Download PDF]
```
