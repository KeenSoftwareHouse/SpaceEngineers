# SECE Contribution Agreement
This agreement is for those wishing to contribute to Space Engineers: Community Edition (SECE).  By submitting a PR or becoming a maintainer, you agree with the SECE Contribution Agreement.

Yes, there's plenty of rules, but complying keeps the spaghetti code away and prevents clogging of the issue tracker.

## Code/Asset Contributions

1. **Contributions must be your own.**  Do not commit code that you have not written. People get lawsuit-happy over plagiarism.
2. **Only submit PRs that are ready to be pulled.**  PRs that are not ready will be closed.
3. **Pull requests must be atomic.**  Change one set of related things at a time.  Bundling sucks for everyone.
4. **Test your changes.**  PRs that do not compile will not be accepted.
5. **Large changes require discussion.**  If you're doing a large, game-changing modification, or a new layout for something, discussion with the community is required.  
  - Texture, shader, and model changes require pictures of before and after.  
  - **MAINTAINERS ARE NOT IMMUNE TO THIS.  GET YOUR ASS IN IRC.**
6. **New public classes, methods, fields, and properties must be documented.**  If you use Visual Studio, simply move your cursor on a blank line above the thing you added and type `///` to get a nice template.
7. **Commit messages should be short, yet meaningful.**  Commits just labelled "lol", for example, are not meaningful.  
8. **PRs must clearly state what they change.**  For example, "I fixed #1234 and added twelve new hats".
9. **PRs must be in their own branches.**  You should develop the PR in its own "feature branch", and then make the PR against the SECE master branch. This helps prevent contamination.

It is also suggested that you hop into irc.rizon.net #sece to discuss your changes, or if you need help.

## Bug Reports
1. Bug report titles must be short, yet meaningful.  "Crash" doesn't mean much.  "Crash when editing world settings" is short, yet meaningful.
2. The body of the bug report must contain:
  * **Commit Hash** - Use `git rev-parse HEAD` in the SECE source code directory to get the hash. "Latest version" means nothing, provide the damn hash.
  * **Description** - Short description of the problem.
  * **Reproduction Steps** - Instructions on how we can produce the same error or bad behavior ourselves.
  * **Expected Behavior** - What you expected the game to do after performing these steps.
  * **Produced Behavior** - What the game actually did.
  * **Log Pastebin** - Use pastebin, hastebin, or some other service to post your SpaceEngineers.log.  It can be found at `%APPDATA%\SpaceEngineers\SpaceEngineers.log`.
    * **Remember to check that you don't have anything secret in that log.** Like, say, passwords or your name.
3. **PATIENCE.**  We are all volunteers, most of us have lives and jobs.  If you start screaming about how no one's handling your bug report, it'll be locked.  If you don't feel it's being processed in a timely fashion, please join IRC and *politely* ask one of us to have a look at it.  If no one's on, try later.

## Feature Suggestions

1. The title of your feature suggestion should tell us what it is. "COOL NEW THING!!!" doesn't mean squat.  "Support for positional teleportation" is OK.
2. **Your feature must be related to a back-end system.**  Example: We are not going to add flying monkeys, but we *may* add things like aerodynamics and other related frameworks.
3. **Feature suggestions must be atomic.**  They must only discuss one related set of features at a time.
4. **Sell us on your feature.**  What would it add?  What benefits could the end-user see?  Could it add things for modders to play with?
  * Do not roleplay as a salesman, that's just tacky.
5. **Discuss potential drawbacks.** How difficult might it be to implement?  Would users hate it?  Would modders hate it?  Would hackers hate it?  Why?
6. **Brace for discussion.**  Features require discussion in order to determine feasibility, manpower required, and implementation details.

## Documentation

1. Proofread.
2. Proofread aloud.
3. Assume your audience has the attention span of a 12-year-old, so make use of illustrations, lists, and formatting to drag them along.
4. Try to keep it PC, 4chan isn't the only site involved in this project.
