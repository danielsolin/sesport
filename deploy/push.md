# Web Push

SESport sends activity reminders through standards-based Web Push. The web
application owns the subscription and sends directly to the browser's push
service. SMTP and the Postfix configuration are unrelated to this feature.

## VAPID keys

Generate one VAPID key pair for SESport. Keep the private key secret and
reuse the same pair for production and development only if that is intended.
For separate environments, generate separate pairs.

One way to generate a pair locally is:

~~~bash
npx --yes web-push generate-vapid-keys
~~~

Put the values in the host-local .env:

~~~text
MemberPush__Subject=mailto:info@sesport.se
MemberPush__PublicKey=<vapid_public_key>
MemberPush__PrivateKey=<vapid_private_key>
~~~

Never commit the private key. The public key is sent to the browser so it can
create a subscription.

## Database and service

Apply the migrations before restarting either web service:

~~~bash
./bin/db-run-migrations.sh
sudo systemctl restart sesport sesport-dev
~~~

The web host needs outbound HTTPS access to browser push endpoints. No
inbound port or DNS record is required for Web Push.

## User flow

On /bevakningar, the page reports whether the current browser has an active
push subscription. The activation action requests notification permission
and saves the browser subscription for the member. Bevaka also registers the
subscription as a fallback. A member can choose one hour, thirty minutes, or
ten minutes before an activity. The default is ten minutes.

The notification worker runs inside each web service but uses database
claiming so the same member/activity reminder is sent only once when multiple
service instances are running.
