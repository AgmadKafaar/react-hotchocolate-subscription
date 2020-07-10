import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import * as serviceWorker from './serviceWorker';
import AuthorizedApolloProvider from "./utils/apolloprovider"
import history from "./utils/history"
import { Auth0Provider } from "./utils/oauth"

const onRedirectCallback = appState => {
  history.push(
    appState && appState.targetUrl
      ? appState.targetUrl
      : window.location.pathname
  )
}

ReactDOM.render(
  <>
    <Auth0Provider
      domain={process.env.REACT_APP_AUTH_DOMAIN}
      client_id={process.env.REACT_APP_AUTH_CLIENT_ID}
      redirect_uri={`${window.location.origin}`}
      onRedirectCallback={onRedirectCallback}
      useRefreshTokens={false}>
      <AuthorizedApolloProvider>
        <App />
      </AuthorizedApolloProvider>
    </Auth0Provider>
  </>,
  document.getElementById('root')
);

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();