git log --since=midnight --numstat --format= | awk '$1 ~ /^[0-9]+$/ { n += $1 + $2 } END { print n+0 }'
