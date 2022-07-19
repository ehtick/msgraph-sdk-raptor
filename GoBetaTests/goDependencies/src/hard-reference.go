// needed for dependabot updates to update go.mod properly
package snippets

import (
    "log"
	msgraphsdk "github.com/microsoftgraph/msgraph-beta-sdk-go"
)

func GetEducationUser() {
	graphClient := msgraphsdk.NewGraphServiceClient(nil)
	_, err := graphClient.Education().Me().User().Get()

    if err != nil {
		log.Fatal(err)
	}
}