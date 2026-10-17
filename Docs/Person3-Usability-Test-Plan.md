# Usability Test Plan and Evidence Log

## Goal
Check that library patrons can search, reserve and borrow at the kiosk without help,
and that staff can use the import and export screens.

## Participants
3 to 5 people who have not seen the system (ideally one older adult and one young person).
Record name or ID, age band, and device used. Get consent to record screens or take notes.

## Tasks (read aloud, give no hints)

| # | Task | Success looks like | Target time |
|---|---|---|---|
| T1 | Find the book "The Hobbit" and say whether you can borrow it | Finds BK001, reads status Borrowed | 1 min |
| T2 | Reserve "The Hobbit" (use member 2, jane.doe@example.com) | Sees queue position | 2 min |
| T3 | At the kiosk, borrow the LEGO set (code TY001) as member 1 | Sees due date | 2 min |
| T4 | At the kiosk, find out what you currently have on loan | Reads loan list | 1 min |
| T5 | (Staff) Import `SampleData/items-sample.csv` and say how many rows were skipped | Says 2 skipped | 3 min |
| T6 | (Manager) Download the fine revenue report as PDF | File downloaded | 2 min |

## Per-task log (copy one table per participant)

| Task | Completed unaided? (Y/N) | Time (s) | Errors / wrong taps | Quote or observation |
|---|---|---|---|---|
| T1 | | | | |
| T2 | | | | |
| T3 | | | | |
| T4 | | | | |
| T5 | | | | |
| T6 | | | | |

## After the tasks
- Rate ease of use 1 (very hard) to 5 (very easy) for: search, reservation, kiosk, staff screens.
- What was confusing? What would you change?

## Results summary (fill in after testing)
- Completion rate per task = successes / participants.
- Average time per task against target.
- Top 3 issues found, the change made, and a before/after screenshot.

## Screenshot checklist for the portfolio
- Public search results with status badges, on desktop and on a phone-sized window
- Item details with the Reserve button, the reserve form, and the confirmation
- Kiosk home, sign in, checkout success, checkout error, account summary
- Reservation queue (Reception) showing Waiting and Notified
- Import upload page, preview with errors, completion page
- Reports page, a report preview, and a downloaded PDF and CSV
