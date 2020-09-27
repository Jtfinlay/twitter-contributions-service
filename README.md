# twitter-contributions-service

Server-side repository to execute [TweetLog](https://tweetlog.azureedge.net/) operations. Uses Azure functions & storage.


### Design

Web client submits username to function that writes to a Queue.

Another function pulls from Queue, calls Twitter APIs for related data, stores data in Table.

Web client polls for data to be available in Table. Once in Table, data is good for 24 hours.


### Setup

Requires .NET, Azure packages, Azure Storage emulator, all that goodness.

Setup local settings with AzureWebJobsStorage pointing to the emulator, and set 'TwitterBearerToken' to the token provided by Twitter.
