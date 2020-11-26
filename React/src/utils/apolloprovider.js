import { ApolloClient, ApolloProvider, HttpLink, InMemoryCache, split } from "@apollo/client"
import { getMainDefinition } from "@apollo/client/utilities"
import { setContext } from "@apollo/client/link/context"
import { WebSocketLink } from "@apollo/client/link/ws"
import React from "react"

import { useAuth0 } from "./oauth"

const AuthorizedApolloProvider = ({ children }) => {
  const { getTokenSilently } = useAuth0()

  const httpLink = new HttpLink({
    uri: "https://" + process.env.REACT_APP_API_URL,
  })

  const wsLink = new WebSocketLink({
    uri: "wss://" + process.env.REACT_APP_API_URL,
    options: {
      reconnect: false,
      connectionParams: async () => {
        const token = await getTokenSilently()
        if (token) {
          return { Authorization: `${token}` }
        }
        return {}
      },
    },
  })

  const authLink = setContext(async () => {
    const token = await getTokenSilently()
    return {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  })

  const splitLink = split(
    ({ query }) => {
      const definition = getMainDefinition(query)
      return (
        definition.kind === "OperationDefinition"
        && definition.operation === "subscription"
      )
    },
    wsLink,
    authLink.concat(httpLink)
  )

  const apolloClient = new ApolloClient({
    link: splitLink,
    cache: new InMemoryCache({
      addTypename: false,
    }),
    connectToDevTools: true,
  })

  return (
    <ApolloProvider client={apolloClient}>
      {children}
    </ApolloProvider>
  )
}

export default AuthorizedApolloProvider