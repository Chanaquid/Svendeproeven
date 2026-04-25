namespace backend.Models
{

    public enum BorrowingStatus
    {
        Free = 0, //score above 50
        AdminApproval = 1, //score above 20 below 50
        Blocked = 2 //below 20
    }

    //Item condition
    public enum ItemCondition
    {
        Excellent = 0, //Like new, no visible wear
        Good = 1, //Minor signs of use, fully functional
        Fair = 2, //Visible wear or cosmetic damage 
        Poor = 3 //a lot of damage but still functional
    }

    //Item status
    public enum ItemStatus
    {
        Pending = 0,  //Awaiting admin approval — not visible to borrowers
        Approved = 1,  //Approved by admin
        Rejected = 2,   //Admin rejected — owner can edit and resubmit
        Deleted = 3 //user soft deletes thier item
    }

    //Item availability
    public enum ItemAvailability
    {
        Available = 0,
        OnRent = 1, //on loan
        Unavailable = 2  //owner manually disabled the listing
    }

    //Loand Status
    public enum LoanStatus
    {
        //Waiting for owner to accept or decline (normal user score)
        Pending = 0,

        //Score is 20–50: Waiting for admin to approve before owner sees it
        AdminPending = 1,

        //Admin approved — borrower can now pick up the item
        Approved = 2,

        //Item has been picked up and loan is in progress
        Active = 3,

        //Item returned befire or after due date
        Completed = 4,

        //Item not returned by due date
        Late = 5,

        //Loan request was declined by admin or owner
        Rejected = 6,

        //Cancelled by borrower before pickup
        Cancelled = 7,

        Extended = 8 //Borrower requested and owner approved extra days
    }

    //Fine enums
    public enum FineType
    {
        ResultedByDispute = 0, //Fine from a dispute

        Custom = 1  //Admin-issued manual fine

    }

    public enum FineStatus
    {
        Unpaid = 0, //Admin placed a fine on the user
        PendingVerification = 1,  //User submitted proof, waiting for admin
        Paid = 2, //Admin approved
        Rejected = 3, //Admin rejected proof, user must resubmit
        Voided = 4 //Fine voided, user dont have to pay 
    }

    //Score enums
    public enum ScoreChangeReason
    {
        OnTimeReturn = 0,  //+5 score for every item
        LateReturn = 1,  //-5 per day up to -25 for every item
        AdminAdjustment = 2,   //Manual override by admin
        ItemDamaged = 3,      //- score after dispute verdict
        LoanCancelled = 4,    //- score for repeat cancellations
        VerificationBonus = 5, //+ score for getting verified
        AppealOutcome = 6, //Admin approves user's score appeal request
        DisputePenalty = 7
    }

    //dispute initiator
    public enum DisputeFiledAs
    {
        AsOwner = 0,  //"The item came back damaged/lost"
        AsBorrower = 1   //"The item was already in this condition when I got it"
    }

    //Given by admin
    public enum DisputeVerdict
    {
        OwnerPenalized = 0, //Fine/score to owner
        BorrowerPenalized = 1,//Fine/score to borrower
        BothPenalized = 2, //Fine/score to both
        NoPenalty = 3 // Waived, no action
    }

    //Dispute enum
    public enum DisputeStatus
    {
        AwaitingResponse = 0,  //Other party has 72h to submit their evidence
        PendingAdminReview = 1,  //Admin is reviewing both sides after other party responded
        Resolved = 2,   //Admin has issued a verdict
        PastDeadline = 3, //72hrs+ without reponse from other party
        Cancelled = 4 //cancelled by the issuer
    }

    //Notif enums
    public enum NotificationType
    {
        //Loan lifecycle
        LoanRequested = 0,
        LoanApproved = 1,
        LoanRejected = 2,
        LoanCancelled = 3,
        LoanActive = 4,   //Item marked as picked up

        //Due date reminders
        DueSoon = 5,   //3 days before due date
        LoanOverdue = 6,   //Day after due date

        //Item lifecycle
        ItemApproved = 7,
        ItemPendingReview = 8,
        ItemRejected = 9,

        //Fines and score
        FineIssued = 10,
        FinePaymentPendingVerification = 11,
        FinePaid = 12,
        FineRejected = 13,
        FineVoided = 14,
        ScoreChanged = 15,

        //Disputes
        DisputeFiled = 16,
        DisputeResponseSubmitted = 17,  //Other party submitted their evidence
        DisputeResolved = 18,

        //Appeals
        AppealSubmitted = 19,
        AppealApproved = 20,
        AppealRejected = 21,

        //Verification
        VerificationApproved = 22,
        VerificationRejected = 23,

        //Messages
        LoanMessageReceived = 24, //New loan chat message
        DirectMessageReceived = 25, //New direct message
        SupportMessageReceived = 26, //New message in support thread

        LoanReturned = 27,
        ItemAvailable = 28,

        //Reports
        ReportSubmitted = 29, //Confirmation to reporter
        ReportResolved = 30, //reporter notified of outcome

        ItemDeleted = 31,

        VerificationSubmitted = 32,
        SupportThreadCreated = 33,
        SupportThreadClosed = 34,
        SupportThreadClaimed = 35,
        DisputeExpired = 36

    }

    //Type of notification
    public enum NotificationReferenceType
    {
        Loan = 0,
        Item = 1,
        Dispute = 2,
        Appeal = 3,
        Fine = 4,
        Verification = 5,
        LoanConversation = 6,
        DirectConversation = 7,
        SupportThread = 8,
        Report = 9,
        Review = 10,
        User = 11,
        ItemFavorites = 12

    }

    public enum MessageType
    {
        LoanMessage = 0,
        DirectMessage = 1,
        SupportMessage = 2
    }

    //Appeal status
    public enum AppealStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3,
        Deleted = 4 //Soft delete
    }

    public enum AppealType
    {
        Score = 0,  //User wants score restored
        Fine = 1    //User wants to appeal a fine
    }

    public enum FineAppealResolution
    {
        Voided = 0, //Fine cancelled entirely
        Custom = 1 //Admin sets a specific amount
    }

    //Verification enums
    public enum VerificationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum VerificationDocumentType
    {
        Passport = 0,
        NationalId = 1,
        DrivingLicense = 2
    }


    public enum ExtensionStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }


    //Support thread status
    public enum SupportThreadStatus
    {
        Open = 0, //Unclaimed — visible to all admins
        Claimed = 1,//One admin owns the conversation
        Closed = 2  //Resolved — can be reopened by admin
    }

    //payment
    public enum PaymentMethod
    {
        MobilePay = 0,
        Card = 1,
        BankTransfer = 2,
        Cash = 3
    }

    //reports
    public enum ReportType
    {
        User = 0, //reporting a user
        Item = 1,//reporting a suspicious/fake item listing
        Review = 2,//reporting an unfair/fake review
        Message = 3 //reporting a message (harassment,abuse etc)
    }

    public enum ReportReason
    {
        //User reports
        FakeIdentity = 0,
        Scammer = 1,
        Harassment = 2,
        InappropriateContent = 3,

        //Item reports
        FakeListing = 4,
        ProhibitedItem = 5,
        MisleadingDescription = 6,

        //General
        Spam = 7,
        Other = 8
    }

    public enum ReportStatus
    {
        Pending = 0,// just submitted, no admin action yet
        UnderReview = 1, // admin claimed it
        Resolved = 2, // admin took action
        Dismissed = 3 // admin reviewed, no action needed
    }




}
