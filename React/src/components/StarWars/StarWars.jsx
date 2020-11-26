import React from "react"
import { useQuery, gql, useMutation, useSubscription } from "@apollo/client"
import { Button } from "@material-ui/core"

const CHARACTER = gql`
  query($id: String!) {
    human(id: $id) {
      id
      name
      height
    }
  }
`

const SUB_REVIEW = gql`
  subscription($episode: Episode!) {
    onReview(episode: $episode) {
      stars
    }
  }
`

const NEW_REVIEW = gql`
  mutation($episode: Episode!, $review: ReviewInput!) {
    createReview(episode: $episode, review: $review) {
      stars
    }
  }
`

const StarWars = () => {
    const [human, setHuman] = React.useState({})
    const { loading, error } = useQuery(CHARACTER, {
        variables: {
          id: "1000",
        },
        onCompleted(data) {
        if (data) {
            setHuman(data.human)
        } else {
            setHuman([])
        }
        },
    })

    useSubscription(SUB_REVIEW, {
        variables: {
          episode: "NEW_HOPE",
        },
        onSubscriptionData: ({ client, subscriptionData }) => {
          console.log("New created DATA!")
          console.log(subscriptionData)
        },
    })

    const [ createReview] = useMutation(NEW_REVIEW, {
        variables: {
          episode: "NEW_HOPE",
          review: {
            commentary: "good",
            stars: 5
          }
        },
        onCompleted(data) {
          if (data) {
            console.log("Add review")
          } else {
            console.log("not added")
          }
        },
      })

    if (error) return <p>Error :(</p>
    if (loading) return <p>Loading</p>

    return (
        <div>
            <p>Human: {human.name}</p>
            <Button onClick={createReview} variant="contained">
                add Review
            </Button>
        </div>
    )
}

export default StarWars